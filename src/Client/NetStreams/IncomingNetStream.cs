
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
        public override long Length => totalCount;
        public override long Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        private RingBuffer<byte> buffer = new RingBuffer<byte>(4 * 1024 * 1024);
        private long readedCount = 0;
        private long totalCount = 0;

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

            if (readedCount == totalCount)
                return Task.FromResult(0);

            currentReading = new StreamReading
            {
                destination = buffer,
                offset = offset,
                count = count,
                taskSource = new TaskCompletionSource<int>()
            };

            return currentReading.taskSource.Task;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            totalCount = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("Cannot write in incoming buffer");
        }

        public void RecievedData(byte[] data)
        {
            buffer.Add(data);
            readedCount += data.Length;
        }

        public void Update()
        {
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
    }

    internal class StreamReading {
        public byte[] destination;
        public int offset;
        public int count;
        public TaskCompletionSource<int> taskSource;
    }
}
