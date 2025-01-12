using MVirus.Server.NetStreams;
using MVirus.Shared.NetPackets;
using System.Threading.Tasks;

namespace MVirus.Server
{
    public class OutcomingNetStreamHandler
    {
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
            var connection = client.netConnection[0] as NetConnectionSimple;

            // This check produces a race condition.
            if (windowSize > 512 && !finished && connection.reliableBufsToSend.Count < 20)
                await SendDataToClient();
        }

        private async Task<int> SendDataToClient()
        {
            int maxReadLen = (int)System.Math.Min(buffer.Length, windowSize);
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
    }
}
