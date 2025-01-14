using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Data.Compression
{
    internal class GZipStreamReversed : Stream
    {
        private readonly Stream _stream;

        public GZipStreamReversed(Stream source)
        {
            _stream = source;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var gzipStream = new GZipStream(destination, CompressionMode.Compress);
            try
            {
                await _stream.CopyToAsync(gzipStream, bufferSize, cancellationToken);
            } finally
            {
                gzipStream.Dispose();
            }
            
        }
    }
}
