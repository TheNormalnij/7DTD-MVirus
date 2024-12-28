
using MVirus.Client.NetStreams;
using MVirus.Server.NetStreams;
using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System;
using System.Collections.Generic;

namespace MVirus.Server
{
    public class OutcomingStreamHandler
    {
        private readonly IStreamSource streamSource;
        private readonly Dictionary<ClientInfo, List<OutcomingNetStreamHandler>> activeRequests = new Dictionary<ClientInfo, List<OutcomingNetStreamHandler>>();

        public OutcomingStreamHandler(IStreamSource _streamSource)
        {
            streamSource = _streamSource;
        }

        public void HandleStreamRequest(ClientInfo sender, string path, byte clientRequestId)
        {
            try
            {
                var req = streamSource.CreateStream(path);
                var outStreamHandler = new OutcomingNetStreamHandler(req.stream, clientRequestId);
            } catch (NetStreamException e) {
                SendStreamError(sender, clientRequestId, e);
            } catch (Exception ex)
            {
                MVLog.Error("Error handling client stream request:" + ex.Message);
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
            var clientStreams = activeRequests[client];

            if (clientStreams == null)
                return;

            foreach (var handler in clientStreams)
                handler.Close();
        }

        private void SendStreamError(ClientInfo client, byte streamId, NetStreamException ex)
        {
            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, ex);
            client.SendPackage(req);
        }

        private void CloseClientStream(ClientInfo client, byte streamId, bool sendMessage = true)
        {
            var stream = GetClientStream(client, streamId);
            stream.Close();
            if (sendMessage)
            {
                var req = NetPackageManager.GetPackage<NetPackageMVirusStreamUpdate>().SetupClose(streamId);
                client.SendPackage(req);
            }
        }

        private OutcomingNetStreamHandler GetClientStream(ClientInfo client, byte streamId)
        {
            var clientStreams = activeRequests[client];

            if (clientStreams == null)
                return null;

            foreach (var handler in clientStreams)
            {
                if (handler.streamId == streamId)
                    return handler;
            }

            return null;
        }
    }
}
