using MVirus.Server.NetStreams;
using MVirus.Shared.NetPackets;
using System.Collections.Generic;
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
            buffer = new byte[1024 * 50];
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

        public int GetAvialableClientStreamWriteSize()
        {
            var connection = client.netConnection[0] as NetConnectionSimple;

            int size = 0;
            var list = new List<NetPackage>();

            connection.GetPackages(list);

            foreach (var package in list)
                size += package.GetLength();

            return 2097152 - size;
        }

        public async Task Update()
        {
            var avialableSize = GetAvialableClientStreamWriteSize();

            while (windowSize > 512 && !finished && avialableSize > 2097152 / 2)
            {
                var writedCount = await SendDataToClient();
                if (writedCount == 0)
                    break;

                avialableSize -= writedCount; 
            }
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
