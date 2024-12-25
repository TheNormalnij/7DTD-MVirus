
using MVirus.Shared.NetPackets;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Client.NetStreams
{
    public class IncomingStreamHandler
    {
        private readonly NetStreamRequest[] activeRequests = new NetStreamRequest[3];

        /// <summary>
        /// Creates a stream with the server
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Task<Stream> CreateFileStream(string path)
        {
            var taskSource = new TaskCompletionSource<Stream>();

            var streamId = GetNextStreamId();

            var request = NetPackageManager.GetPackage<NetPackageMVirusCreateFileStream>().Setup(path, streamId);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);

            return taskSource.Task;
        }

        private byte GetNextStreamId()
        {
            for (int i = 0; i < activeRequests.Length; i++) {
                if (activeRequests[i] == null)
                    return (byte)i;
            }
            throw new NetStreamException("Stream limit reached");
        }

        public void HandleIncomingData(byte streamId, byte[] data)
        {
            var request = activeRequests[streamId];
            if (request == null)
            {
                SendStreamError(streamId, FileStreamErrorCode.NOT_FOUND);
                return;
            }

            request.stream.RecievedData(data);
        }

        public void HandleStreamOpen(byte streamId, long streamSize)
        {
            var request = activeRequests[streamId];
            if (request == null)
            {
                SendStreamError(streamId, FileStreamErrorCode.NOT_FOUND);
                return;
            }

            if (request.status != StreamStatus.CREATING)
            {
                SendStreamError(streamId, FileStreamErrorCode.INVALID_STATE);
                return;
            }

            request.stream.SetLength(streamSize);

            request.creatingTask.SetResult(request.stream);
        }

        public void HandleError(byte streamId, FileStreamErrorCode code)
        {
            var stream = activeRequests[streamId];
            if (stream == null)
            {
                Log.Error("[MVirus] Unknown stream error: " + code.ToString());
                return;
            }

            if (stream.status == StreamStatus.CREATING)
            {
                stream.creatingTask.SetException(new NetStreamException("Stream error: " + code.ToString()));
                return;
            }
        }

        private void SendStreamError(byte streamId, FileStreamErrorCode code)
        {
            var request = NetPackageManager.GetPackage<NetPackageMVirusFileStreamError>().Setup(streamId, code);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);
        }
    }

    public enum StreamStatus
    {
        CREATING,
        READING,
        CLOSING,
        FINISHED
    }

    public class NetStreamRequest
    {
        public IncomingNetStream stream;
        public StreamStatus status;
        public TaskCompletionSource<Stream> creatingTask;
    }
}
