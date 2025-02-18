﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Logger;

namespace MVirus.Client.NetStreams
{
    public class IncomingNetStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => TotalCount;
        public override long Position { get => readCount; set => throw new NotImplementedException(); }

        public bool GzipCompressed { get; private set; } = false;
        public int BufferAvailableSize => buffer.FreeSize;
        public long SendedCount { get; private set; } = 0;
        public long TotalCount { get; private set; } = -1;
        private long readCount = 0;
        public Action<IncomingNetStream> closeCallback;

        private readonly object dataLock = new object();

        private Exception exception = null;

        private readonly Data.RingBuffer<byte> buffer;
        private StreamReading currentReading;

        public IncomingNetStream(bool compressed, int bufferSize)
        {
            GzipCompressed = compressed;
            buffer = new Data.RingBuffer<byte>(bufferSize);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (currentReading != null)
                throw new Exception("Reading in progress");

            if (readCount == TotalCount)
                return Task.FromResult(0);

            if (exception != null && (TotalCount == -1 || readCount == TotalCount))
                return Task.FromException<int>(exception);

            var newReading = new StreamReading
            {
                destination = buffer,
                offset = offset,
                count = count,
                taskSource = new TaskCompletionSource<int>(),
            };

            lock (dataLock)
            {
                currentReading = newReading;
                newReading.cancellationRegistration = cancellationToken.Register(DoCancellation);

                if (exception != null)
                    return Task.FromException<int>(exception);

                DoReading();
            }

            return newReading.taskSource.Task;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            TotalCount = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("Cannot write in incoming stream");
        }

        public void RecievedData(byte[] data)
        {
            lock (dataLock)
            {
                buffer.Add(data);
                SendedCount += data.Length;
                DoReading();
            }
        }

        private void DoReading()
        {
            if (buffer.Count == 0)
                return;

            var reading = currentReading;
            if (reading == null || reading.taskSource.Task.IsCompleted)
                return;

            reading.cancellationRegistration.Dispose();

            currentReading = null;

            var transferBytes = Math.Min(reading.count, buffer.Count);

            buffer.CopyTo(reading.destination, reading.offset, transferBytes);

            buffer.Discard(transferBytes);

            reading.taskSource.SetResult(transferBytes);

            readCount += transferBytes;
        }

        private void DoCancellation()
        {
            SetException(new TaskCanceledException());
        }

        public void SetException(Exception ex)
        {
            lock (dataLock)
            {
                exception = ex;

                if (currentReading == null || currentReading.taskSource.Task.IsCompleted)
                    return;

                currentReading.taskSource.SetException(ex);
                currentReading.cancellationRegistration.Dispose();
                currentReading = null;
            }
        }

        public bool IsAllDataRecieved()
        {
            return TotalCount <= SendedCount;
        }

        public void Finished()
        {
            TotalCount = SendedCount;
            lock (dataLock)
            {
                var reading = currentReading;
                if (reading == null || reading.taskSource.Task.IsCompleted)
                    return;

                if (readCount != TotalCount)
                    return;

                reading.taskSource.SetResult(0);
            }
        }

        public override void Close()
        {
            closeCallback?.Invoke(this);
            base.Close();
        }
    }

    internal class StreamReading {
        public byte[] destination;
        public int offset;
        public int count;
        public TaskCompletionSource<int> taskSource;
        public CancellationTokenRegistration cancellationRegistration;
    }
}
