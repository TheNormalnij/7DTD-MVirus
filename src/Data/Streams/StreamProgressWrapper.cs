using System;
using System.IO;

namespace MVirus.Data.Streams
{
    public class StreamProgressWrapper : Stream
    {
        private readonly Stream stream;
        private readonly Action<int> progress;

        public StreamProgressWrapper(Stream _stream, Action<int> _progress)
        {
            stream = _stream;
            progress = _progress;
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var size = stream.Read(buffer, offset, count);
            progress.Invoke(size);
            return size;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }
    }
}
