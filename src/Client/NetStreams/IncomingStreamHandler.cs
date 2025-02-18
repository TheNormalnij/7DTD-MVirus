﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Logger;
using MVirus.NetPackets;
using MVirus.NetStreams;

namespace MVirus.Client.NetStreams
{
    public class IncomingStreamHandler
    {
        private const int STREAM_CREATE_OPEN_INTERVAL_MS = 15000;
        private bool streamsSynced = false;
        private readonly StreamPool streamPool = new StreamPool();

        /// <summary>
        /// Creates a stream with the server
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IncomingNetStream> CreateFileStream(string path, CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<IncomingNetStream>();

            var req = new NetStreamRequest
            {
                stream = null,
                status = StreamStatus.CREATING,
                creatingTask = taskSource,
                lastUpdateTick = Environment.TickCount,
                path = path,
            };

            var streamId = streamPool.Add(req);

            req.cancellationRegistration = cancellationToken.Register(() =>
            {
                if (taskSource.Task.IsCompleted)
                    return;

                MVLog.Debug($"Stream request is canceled {streamId}");
                taskSource.SetCanceled();
                streamPool.Remove(streamId);
            });

            SendStreamRequest(path, streamId);

            return taskSource.Task;
        }

        public void HandleIncomingData(byte streamId, byte[] data)
        {
            MVLog.Debug($"Got data: {streamId} {data.Length}");
            var request = streamPool.Get(streamId);
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
        }

        public void HandleStreamOpen(byte streamId, long streamSize, bool compressed)
        {
            MVLog.Debug($"Stream opened: {streamId} compressed {compressed}" );
            var request = streamPool.Get(streamId);
            if (request == null)
            {
                MVLog.Debug($"Not found {streamId}");
                SendStreamError(streamId, StreamErrorCode.NOT_FOUND);
                return;
            }

            if (request.status != StreamStatus.CREATING)
            {
                MVLog.Debug($"Invalid status {streamId}");
                SendStreamError(streamId, StreamErrorCode.INVALID_STATE);
                return;
            }

            request.cancellationRegistration.Dispose();

            request.stream = new IncomingNetStream(
                compressed: compressed,
                bufferSize: (int)Math.Min(20 * 1024 * 1024, streamSize)
            );
            request.stream.SetLength(streamSize);
            request.stream.closeCallback = stream =>
            {
                MVLog.Debug($"Stream closed in closeCallback {streamId}");
                streamPool.Remove(streamId);
            };
            request.creatingTask.SetResult(request.stream);
            request.status = StreamStatus.READING;

            if (!streamsSynced)
                StartStreamSyncing();
        }

        public void HandleStreamClose(byte streamId, bool finished)
        {
            MVLog.Debug("Stream closed: " + streamId);

            var request = streamPool.Get(streamId);
            if (request == null)
                return;

            switch (request.status)
            {
                case StreamStatus.CREATING:
                    {
                        request.creatingTask.SetException(new NetStreamException("Closed by server"));
                        break;
                    }
                case StreamStatus.READING:
                    {
                        if (finished)
                            request.stream.Finished();
                        else
                            request.stream.SetException(new NetStreamException("Closed by server"));

                        break;
                    }
                default:
                    break;
            }

            streamPool.Remove(streamId);
        }

        public void HandleError(byte streamId, StreamErrorCode code)
        {
            MVLog.Debug($"Stream error: {streamId} {code}");

            var stream = streamPool.Get(streamId);
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

        private void SendStreamRequest(string path, byte streamId)
        {
            MVLog.Debug($"Send stream request {streamId} {path}");
            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamCreate>().Setup(path, streamId);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request);
        }

        private void SyncStreams()
        {
            var syncData = new List<NetStreamSyncData>();
            var currentTick = Environment.TickCount;

            streamPool.Foreach((NetStreamRequest req) =>
            {
                if (req.status == StreamStatus.CREATING)
                {
                    if (currentTick > req.lastUpdateTick + STREAM_CREATE_OPEN_INTERVAL_MS)
                    {
                        SendStreamRequest(req.path, req.streamId);
                        req.lastUpdateTick = currentTick;
                    }
                }
                else if (req.status == StreamStatus.READING)
                {
                    syncData.Add(new NetStreamSyncData
                    {
                        streamId = req.streamId,
                        readedCount = req.stream.SendedCount,
                        bufferSize = req.stream.BufferAvailableSize,
                    });
                }
            });

            if (syncData.Count == 0)
                return;

            MVLog.Debug("Send sync for " + syncData.Select(item => item.streamId).Join());

            var request = NetPackageManager.GetPackage<NetPackageMVirusStreamSync>().Setup(syncData);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(request, true);
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
                await Task.Delay(1000);
            }
        }

        private bool HasActiveStreams()
        {
            return !streamPool.IsEmpty();
        }
    }
}
