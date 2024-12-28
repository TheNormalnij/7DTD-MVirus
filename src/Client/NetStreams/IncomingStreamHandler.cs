﻿
using MVirus.Shared.NetPackets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Client.NetStreams
{
    public class IncomingStreamHandler
    {
        private readonly NetStreamRequest[] activeRequests = new NetStreamRequest[3];
        private bool streamsSynced = false;

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
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            request.stream.RecievedData(data);

            if (request.stream.IsAllDataRecieved())
            {
                request.status = StreamStatus.FINISHED;
                SendStreamFinished(streamId);
            }
        }

        public void HandleStreamOpen(byte streamId, long streamSize)
        {
            var request = activeRequests[streamId];
            if (request == null)
            {
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            if (request.status != StreamStatus.CREATING)
            {
                SendStreamError(streamId, StreamErrorCode.INVALID_STATE);
                return;
            }

            request.stream.SetLength(streamSize);

            request.creatingTask.SetResult(request.stream);
        }

        public void HandleError(byte streamId, StreamErrorCode code)
        {
            var stream = activeRequests[streamId];
            if (stream == null)
            {
                Log.Error("[MVirus] Unknown stream error: " + code.ToString());
                return;
            }

            var exception = new NetStreamException("Stream error: " + code.ToString());

            if (stream.status == StreamStatus.CREATING)
            {
                stream.creatingTask.SetException(exception);
                return;
            }
        }

        private void SendStreamFinished(byte streamId)
        {

        }

        private void SendStreamError(byte streamId, StreamErrorCode code)
        {
            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, code);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);
        }

        private void SyncStreams()
        {
            List<NetStreamSyncData> syncData = new List<NetStreamSyncData>();

            for (int streamId = 0; streamId < activeRequests.Length; streamId++)
            {
                var req = activeRequests[streamId];
                if (req == null)
                {
                    syncData.Add(new NetStreamSyncData
                    {
                        streamId = streamId,
                        readedCount = req.stream.ReadedCount,
                        bufferSize = req.stream.BufferAvialableSize,
                    });
                }
            }
        }

        private void StartStreamSyncing()
        {
            streamsSynced = true;
            Task.Run(DoStreamSyncingLoop);
        }

        private void StopStreamSyncing()
        {
            streamsSynced = false;
        }

        private async Task DoStreamSyncingLoop()
        {
            while (streamsSynced)
            {
                SyncStreams();
                if (HasActiveStreams())
                {
                    StopStreamSyncing();
                    return;
                }
                await Task.Delay(50);
            }
        }

        private bool HasActiveStreams()
        {
            foreach (var req in activeRequests)
            {
                if (req.status == StreamStatus.READING)
                    return true;
            }

            return false;
        }
    }

    public enum StreamStatus
    {
        CREATING,
        READING,
        CLOSING,
        FINISHED
    }

    public class NetStreamSyncData
    {
        public int streamId;
        public long readedCount;
        public long bufferSize;
    }

    public class NetStreamRequest
    {
        public IncomingNetStream stream;
        public StreamStatus status;
        public TaskCompletionSource<Stream> creatingTask;
    }
}
