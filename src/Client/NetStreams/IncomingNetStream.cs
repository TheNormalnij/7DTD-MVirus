
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client.NetStreams
{
    public class IncomingNetStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => TotalCount;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int BufferAvialableSize => buffer.FreeSize;
        public long ReadedCount { get; private set; }
        public long TotalCount { get; private set; } = -1;

        private Exception exception = null;

        private RingBuffer<byte> buffer = new RingBuffer<byte>(4 * 1024 * 1024);
        private StreamReading currentReading;

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

            if (ReadedCount == TotalCount)
                return Task.FromResult(0);

            if (exception != null)
                return Task.FromException<int>(exception);

            currentReading = new StreamReading
            {
                destination = buffer,
                offset = offset,
                count = count,
                taskSource = new TaskCompletionSource<int>()
            };

            DoReading();

            return currentReading.taskSource.Task;
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
            buffer.Add(data);
            ReadedCount += data.Length;
            DoReading();
        }

        private void DoReading()
        {
            if (buffer.Count == 0)
                return;

            if (currentReading == null || currentReading.taskSource.Task.IsCompleted)
                return;

            var readCount = Math.Min(currentReading.count, buffer.Count);
            buffer.CopyTo(currentReading.destination, currentReading.offset, readCount);
            buffer.Discard(readCount);
            currentReading.taskSource.SetResult(readCount);
        }

        public void SetException(Exception ex)
        {
            exception = ex;

            if (currentReading == null || currentReading.taskSource.Task.IsCompleted)
                return;

            currentReading.taskSource.SetException(ex);
        }

        public bool IsAllDataRecieved()
        {
            return TotalCount <= ReadedCount;
        }
    }

    internal class StreamReading {
        public byte[] destination;
        public int offset;
        public int count;
        public TaskCompletionSource<int> taskSource;
    }
}
