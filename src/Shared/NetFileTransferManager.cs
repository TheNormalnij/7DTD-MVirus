using MVirus.Shared.NetPackets;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Shared
{
    public class NetFileTransferManager
    {
        private 

        public NetFileTransferManager() { }

        /// <summary>
        /// Creates a stream with the server
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Task<Stream> CreateFileStream(string path)
        {
            var taskSource = new TaskCompletionSource<Stream>();

            var streamId = GetNextStreamId();

            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMVirusCreateFileStream>().Setup(path, GetNextStreamId()));
            return taskSource.Task;
        }

        public void HandleStreamRequest(ClientInfo sender, string path, byte clientRequestId) {

        }

        public void HandleStreamError(ClientInfo sender, byte clientRequestId, FileStreamErrorCode code)
        {

        }

        private byte GetNextStreamId()
        {
            return 0;
        }
    }
}
