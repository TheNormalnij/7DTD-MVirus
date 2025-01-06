
using MVirus.Shared;
using MVirus.Shared.NetPackets;
using MVirus.Shared.NetStreams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVirus.Client.NetStreams
{
    public class IncomingStreamHandler
    {
        private const long STREAM_POOL_SIZE = 10;
        private readonly NetStreamRequest[] activeRequests = new NetStreamRequest[STREAM_POOL_SIZE];
        private int lastStreamId = -1;

        private bool streamsSynced = false;

        /// <summary>
        /// Creates a stream with the server
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Task<IncomingNetStream> CreateFileStream(string path)
        {

            var taskSource = new TaskCompletionSource<IncomingNetStream>();

            var streamId = GetNextStreamId();
            MVLog.Debug($"Create new stream {streamId} {path}");

            activeRequests[streamId] = new NetStreamRequest
            {
                stream = null,
                status = StreamStatus.CREATING,
                creatingTask = taskSource,
            };

            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamCreate>().Setup(path, streamId);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);

            return taskSource.Task;
        }

        private byte GetNextStreamId()
        {
            for (int i = lastStreamId + 1; i < activeRequests.Length; i++) {
                if (activeRequests[i] == null)
                {
                    lastStreamId = i;
                    return (byte)i;
                }
            }
            for (int i = 0; i < lastStreamId; i++)
            {
                if (activeRequests[i] == null)
                {
                    lastStreamId = i;
                    return (byte)i;
                }
            }
            throw new NetStreamException("Stream limit reached");
        }

        public void HandleIncomingData(byte streamId, byte[] data)
        {
            MVLog.Debug($"Got data: {streamId} {data.Length}");
            var request = activeRequests[streamId];
            if (request == null)
            {
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            if (request.status != StreamStatus.READING)
            {
                MVLog.Debug($"Invalid state {streamId}");
                SendStreamError(streamId, StreamErrorCode.INVALID_STATE);
                return;
            }

            request.stream.RecievedData(data);

/*            if (request.stream.IsAllDataRecieved())
            {
                request.status = StreamStatus.FINISHED;
            }*/
        }

        public void HandleStreamOpen(byte streamId, long streamSize, bool compressed)
        {
            MVLog.Debug($"Stream oppened: {streamId} compressed {compressed}" );
            var request = activeRequests[streamId];
            if (request == null)
            {
                MVLog.Debug($"Not found {streamId}");
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            if (request.status != StreamStatus.CREATING)
            {
                MVLog.Debug($"Invalid state {streamId}");
                SendStreamError(streamId, StreamErrorCode.INVALID_STATE);
                return;
            }

            request.stream = new IncomingNetStream();
            request.stream.GzipCompressed = compressed;
            request.stream.SetLength(streamSize);
            request.creatingTask.SetResult(request.stream);
            request.status = StreamStatus.READING;

            if (!streamsSynced)
                StartStreamSyncing();
        }

        public void HandleStreamClose(byte streamId)
        {
            MVLog.Debug("Stream closed: " + streamId);

            var request = activeRequests[streamId];
            if (request == null)
            {
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            switch (request.status)
            {
                case StreamStatus.CREATING:
                    {
                        request.creatingTask.SetException(new NetStreamException("Closed by server"));
                        break;
                    }
                case StreamStatus.READING:
                    {
                        request.stream.SetException(new NetStreamException("Closed by server"));
                        break;
                    }
                case StreamStatus.CLOSING:
                    {
                        request.status = StreamStatus.FINISHED;
                        break;
                    }
                default:
                    break;
            }

            activeRequests[streamId] = null;
        }

        public void HandleError(byte streamId, StreamErrorCode code)
        {
            MVLog.Debug($"Stream error: {0} {1}", streamId, code.ToString());

            var stream = activeRequests[streamId];
            if (stream == null)
            {
                MVLog.Error("Unknown stream error: " + code.ToString());
                return;
            }

            var exception = new NetStreamException("Stream error: " + code.ToString());

            if (stream.status == StreamStatus.CREATING)
            {
                stream.creatingTask.SetException(exception);
                return;
            }
        }

        private void SendStreamError(byte streamId, StreamErrorCode code)
        {
            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, new NetStreamException(code));
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);
        }

        private void SyncStreams()
        {
            var syncData = new List<NetStreamSyncData>();

            for (int streamId = 0; streamId < STREAM_POOL_SIZE; streamId++)
            {
                var req = activeRequests[streamId];
                if (req != null && req.status == StreamStatus.READING)
                {
                    syncData.Add(new NetStreamSyncData
                    {
                        streamId = (byte)streamId,
                        readedCount = req.stream.SendedCount,
                        bufferSize = req.stream.BufferAvialableSize,
                    });
                }
            }

            if (syncData.Count == 0)
                return;

            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamSync>().Setup(syncData);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);
        }

        private void StartStreamSyncing()
        {
            MVLog.Debug("Start stream syncing loop");
            streamsSynced = true;
            Task.Run(DoStreamSyncingLoop);
        }

        private void StopStreamSyncing()
        {
            MVLog.Debug("Stop stream syncing loop");
            streamsSynced = false;
        }

        private async Task DoStreamSyncingLoop()
        {
            while (streamsSynced)
            {
                try
                {
                    SyncStreams();
                    if (!HasActiveStreams())
                    {
                        StopStreamSyncing();
                        return;
                    }
                } catch (Exception ex) {
                    MVLog.Exception(ex);
                }
                await Task.Delay(200);
            }
        }

        private bool HasActiveStreams()
        {
            foreach (var req in activeRequests)
            {
                if (req != null && (req.status == StreamStatus.READING || req.status == StreamStatus.CREATING))
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

    public class NetStreamRequest
    {
        public IncomingNetStream stream;
        public StreamStatus status;
        public TaskCompletionSource<IncomingNetStream> creatingTask;
    }
}
