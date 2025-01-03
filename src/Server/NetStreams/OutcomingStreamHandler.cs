
using MVirus.Shared.NetStreams;
using MVirus.Server.NetStreams;
using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MVirus.Server
{
    public class OutcomingStreamHandler
    {
        private readonly IStreamSource streamSource;
        private readonly OutgoingStreamStore activeRequest = new OutgoingStreamStore();
        private bool enabled;

        public OutcomingStreamHandler(IStreamSource _streamSource)
        {
            streamSource = _streamSource;
            enabled = true;

            Task.Run(UpdateLoop);
        }

        public void HandleStreamRequest(ClientInfo sender, string path, byte clientRequestId)
        {
            if (!enabled)
            {
                // Ignore everything
                // Do not send an error
                return;
            }

            try
            {
                var req = streamSource.CreateStream(path);
                var outStreamHandler = new OutcomingNetStreamHandler(sender, req.stream, clientRequestId);
                activeRequest.Add(sender, outStreamHandler);
                SendStreamCreated(sender, outStreamHandler);
            } catch (NetStreamException e) {
                SendStreamError(sender, clientRequestId, e);
            } catch (Exception ex)
            {
                MVLog.Error("Error handling client stream request: " + ex.Message);
                SendStreamError(sender, clientRequestId, new NetStreamException(ex.Message));
            }
        }

        public void HandleStreamError(ClientInfo sender, byte clientRequestId, StreamErrorCode code)
        {
            MVLog.Error("Client sends stream error: " + code.ToString());
            CloseClientStream(sender, clientRequestId, false);
        }

        public void CloseAllClientStreams(ClientInfo client)
        {
            foreach (var handler in activeRequest.GetStreams(client))
                handler.Close();

            activeRequest.Remove(client);
        }

        private void SendStreamError(ClientInfo client, byte streamId, NetStreamException ex)
        {
            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, ex);
            client.SendPackage(req);
        }

        private void SendStreamCreated(ClientInfo client, OutcomingNetStreamHandler streamHandler)
        {
            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamUpdate>().SetupCreate(streamHandler.streamId, streamHandler.stream.Length);
            client.SendPackage(req);
        }

        private void CloseClientStream(ClientInfo client, byte streamId, bool sendMessage = true)
        {
            var stream = activeRequest.GetClientStream(client, streamId);
            stream.Close();
            if (sendMessage)
            {
                var req = NetPackageManager.GetPackage<NetPackageMVirusStreamUpdate>().SetupClose(streamId);
                client.SendPackage(req);
            }

            activeRequest.Remove(client, stream);
        }

        private async Task UpdateLoop()
        {
            while (enabled)
            {
                try
                {
                    var removeList = new List<OutcomingNetStreamHandler>();
                    foreach (var client in activeRequest.GetActiveClients())
                    {
                        foreach (var handler in activeRequest.GetStreams(client))
                        {
                            await handler.Update();
                            if (handler.finished)
                                removeList.Add(handler);
                        }
                    }

                    foreach (var handler in removeList)
                    {
                        CloseClientStream(handler.client, handler.streamId, true);
                    }
                }
                catch (Exception ex)
                {
                    MVLog.Exception(ex);
                }

                await Task.Delay(5);
            }
        }

        public void Stop()
        {
            if (!enabled)
                return;

            foreach (var client in activeRequest.GetActiveClients())
                CloseAllClientStreams(client);

            enabled = false;
        }
    }
}
