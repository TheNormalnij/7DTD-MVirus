using MVirus.Shared.NetPackets;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Server
{
    public class OutcomingNetStreamHandler
    {
        private readonly byte[] buffer;
        private readonly Stream stream;
        public readonly ClientInfo client;
        public readonly byte streamId;

        public OutcomingNetStreamHandler(ClientInfo _client, Stream sourceStream, byte clientId)
        {
            client = _client;
            stream = sourceStream;
            streamId = clientId;
            buffer = new byte[4096];
        }

        public void Close() {
            stream.Close();
        }

        private async Task Update()
        {

            await SendDataToClient();
        }

        private async Task SendDataToClient()
        {
            var readedCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (readedCount == 0)
            {
                return;
            }

            var req = NetPackageManager.GetPackage<NetPackageMVirusStreamData>().Setup(streamId, buffer, readedCount);
            client.SendPackage(req);
        }
    }
}
