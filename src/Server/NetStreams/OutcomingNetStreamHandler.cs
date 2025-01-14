using MVirus.Server.NetStreams;
using MVirus.Shared.NetPackets;
using System;
using System.Threading.Tasks;
using UniLinq;

namespace MVirus.Server
{
    public class OutcomingNetStreamHandler
    {
        private const int RELIABLE_BUFFER_LIMIT = 500 * 1024;
        private long sendedCount = 0;
        private long windowSize = 1024 * 1024 * 20;

        private readonly byte[] buffer;
        public readonly RequestedStreamParams req;
        public readonly ClientInfo client;
        public readonly byte streamId;
        public bool finished;

        public OutcomingNetStreamHandler(ClientInfo _client, RequestedStreamParams _req, byte clientId)
        {
            client = _client;
            req = _req;
            streamId = clientId;
            buffer = new byte[100 * 1024];
            finished = false;
            sendedCount = 0;
        }

        public void Close() {
            req.Close();
        }

        public void UpdateWindowSize(long clientReaded, int clientBuffer)
        {
            windowSize = (clientReaded + clientBuffer) - sendedCount;
        }

        public async Task Update()
        {
            while (windowSize > 512 && !finished)
                await SendDataToClient();
        }

        private async Task<int> SendDataToClient()
        {
            // Small race condition here
            var allowedToWrite = GetAllowedSizeToWrite();
            int maxReadLen = Math.Min((int)Math.Min(buffer.Length, windowSize), allowedToWrite);
            var readedCount = await req.stream.ReadAsync(buffer, 0, maxReadLen);
            if (readedCount == 0)
            {
                finished = true;
                return 0;
            }

            windowSize -= readedCount;
            sendedCount += readedCount;

            var packet = NetPackageManager.GetPackage<NetPackageMVirusStreamData>().Setup(streamId, buffer, readedCount);
            client.SendPackage(packet);

            return readedCount;
        }

        private int GetAllowedSizeToWrite()
        {
            var connection = client.netConnection[0] as NetConnectionSimple;

            int size = 0;
            foreach (var buf in connection.reliableBufsToSend.ToArray())
                size += buf.Count;

            return RELIABLE_BUFFER_LIMIT - size;
        }
    }
}
