using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Server
{
    public class OutcomingNetStreamHandler
    {
        private long sendedCount = 0;
        private long windowSize = 1024 * 50;

        private readonly byte[] buffer;
        public readonly Stream stream;
        public readonly ClientInfo client;
        public readonly byte streamId;
        public bool finished;


        public OutcomingNetStreamHandler(ClientInfo _client, Stream sourceStream, byte clientId)
        {
            client = _client;
            stream = sourceStream;
            streamId = clientId;
            buffer = new byte[1024 * 50];
            finished = false;
            sendedCount = 0;
        }

        public void Close() {
            stream.Close();
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
            var readedCount = await stream.ReadAsync(buffer, 0, maxReadLen);
            if (readedCount == 0)
            {
                finished = true;
                return 0;
            }

            windowSize -= readedCount;
            sendedCount += readedCount;

            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamData>().Setup(streamId, buffer, readedCount);
            client.SendPackage(req);

            return readedCount;
        }
    }
}
