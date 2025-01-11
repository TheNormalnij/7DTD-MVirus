using System.Threading.Tasks;

namespace MVirus.Client.NetStreams
{
    internal enum StreamStatus
    {
        CREATING,
        READING,
    }

    internal class NetStreamRequest
    {
        public IncomingNetStream stream;
        public StreamStatus status;
        public TaskCompletionSource<IncomingNetStream> creatingTask;
        public int lastUpdateTick;
        public string path;
        public byte streamId;
    }
}
