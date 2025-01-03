using MVirus.Shared.NetPackets;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Server
{
    public class OutcomingNetStreamHandler
    {
        private NetPackageMVirusStreamData activePackage;

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
        }

        public void Close() {
            stream.Close();
        }

        public async Task Update()
        {
            var connection = client.netConnection[0] as NetConnectionSimple;
            var streamPosition = connection.reliableSendStreamWriter.BaseStream.Position;
            if (activePackage == null || activePackage.WasWritted)
                await SendDataToClient();
        }

        private async Task SendDataToClient()
        {
            var readedCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (readedCount == 0)
            {
                finished = true;
                return;
            }

            activePackage = NetPackageManager.GetPackage<NetPackageMVirusStreamData>().Setup(streamId, buffer, readedCount);
            client.SendPackage(activePackage);
        }
    }
}
