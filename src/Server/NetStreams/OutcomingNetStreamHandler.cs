using MVirus.Shared;
using MVirus.Shared.NetPackets;
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

        public async Task Update()
        {
            //var connection = client.netConnection[0] as NetConnectionSimple;
            //var streamPosition = connection.reliableSendStreamWriter.BaseStream.Position;

            while (windowSize > 512 && !finished)
                await SendDataToClient();
        }

        private async Task SendDataToClient()
        {
            int maxReadLen = (int)System.Math.Min(buffer.Length, windowSize);
            var readedCount = await stream.ReadAsync(buffer, 0, maxReadLen);
            if (readedCount == 0)
            {
                finished = true;
                return;
            }

            windowSize -= readedCount;
            sendedCount += readedCount;

            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamData>().Setup(streamId, buffer, readedCount);
            client.SendPackage(req);
        }
    }
}
