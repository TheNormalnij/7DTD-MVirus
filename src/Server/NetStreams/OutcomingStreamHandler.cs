﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MVirus.Logger;
using MVirus.NetPackets;
using MVirus.NetStreams;
using MVirus.Server.NetStreams.StreamSource;

namespace MVirus.Server.NetStreams
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
                var currentHandler = activeRequest.GetClientStream(sender, clientRequestId);
                if (currentHandler != null)
                    return;

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
            MVLog.Error($"Client sends stream {clientRequestId} error: {code}");
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

        private void CloseAllClientStreams(ClientInfo client)
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

        private void SendStreamClosed(ClientInfo client, byte streamId, bool finished)
        {
            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamClosed>().Setup(streamId, finished);
            client.SendPackage(req);
        }

        private void CloseClientStream(ClientInfo client, byte streamId, bool sendMessage = true)
        {
            MVLog.Debug($"Close stream {streamId}");

            var stream = activeRequest.GetClientStream(client, streamId);
            if (stream == null)
            {
                MVLog.Debug($"Cannot close stream {streamId}. It's null");
                return;
            }

            stream.Close();
            if (sendMessage)
                SendStreamClosed(client, streamId, stream.finished);

            activeRequest.Remove(client, stream);
        }

        private async Task UpdateLoop()
        {
            while (enabled)
            {
                try
                {
                    foreach (var client in activeRequest.GetActiveClients())
                    {
                        if (client.netConnection[0].IsDisconnected())
                        {
                            foreach (var handler in activeRequest.GetStreams(client))
                                CloseClientStream(handler.client, handler.streamId, false);
                        }

                        foreach (var handler in activeRequest.GetStreams(client))
                        {
                            await handler.Update();
                            if (handler.finished)
                                CloseClientStream(handler.client, handler.streamId, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MVLog.Exception(ex);
                }

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
