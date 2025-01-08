
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

            MVLog.Debug($"New stream request {clientRequestId} {path}");

            try
            {
                var req = streamSource.CreateStream(path);
                var outStreamHandler = new OutcomingNetStreamHandler(sender, req, clientRequestId);
                activeRequest.Add(sender, outStreamHandler);
                SendStreamCreated(sender, clientRequestId, req);
            } catch (NetStreamException ex)
            {
                MVLog.Error("HandleStreamRequest NetStreamException: " + ex.Message);
                SendStreamError(sender, clientRequestId, ex);
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

        public void HandleSyncData(ClientInfo sender, List<NetStreamSyncData> data)
        {
            MVLog.Debug($"Sync data: {data.Count}");
            foreach (NetStreamSyncData syncData in data)
            {
                var handler = activeRequest.GetClientStream(sender, syncData.streamId);
                handler?.UpdateWindowSize(syncData.readedCount, syncData.bufferSize);
            }
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

        private void SendStreamCreated(ClientInfo client, byte streamId, RequestedStreamParams streamParams)
        {
            MVLog.Debug("Created stream size: " + streamParams.length);
            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamCreated>().Setup(streamId, streamParams.length, streamParams.compressed);
            client.SendPackage(req);
        }

        private void CloseClientStream(ClientInfo client, byte streamId, bool sendMessage = true)
        {
            MVLog.Debug($"Close stream {streamId}");

            var stream = activeRequest.GetClientStream(client, streamId);
            stream.Close();
            if (sendMessage)
            {
                var req = NetPackageManager.GetPackage<NetPackageMVirusStreamClosed>().Setup(streamId, stream.finished);
                client.SendPackage(req);
            }

            activeRequest.Remove(client, stream);
        }

        private async Task UpdateLoop()
        {
            while (enabled)
            {
                var removeList = new List<OutcomingNetStreamHandler>();
                foreach (var client in activeRequest.GetActiveClients())
                {
                    if (client.netConnection[0].IsDisconnected())
                    {
                        foreach (var handler in activeRequest.GetStreams(client))
                            removeList.Add(handler);
                    }
                    try
                    {
                        foreach (var handler in activeRequest.GetStreams(client))
                        {
                            await handler.Update();
                            if (handler.finished)
                                removeList.Add(handler);
                        }
                    } catch (Exception ex) {
                        MVLog.Exception(ex);
                    }
                }

                foreach (var handler in removeList)
                    CloseClientStream(handler.client, handler.streamId, true);

                await Task.Delay(50);
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
