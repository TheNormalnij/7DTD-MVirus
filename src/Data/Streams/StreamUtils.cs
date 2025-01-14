using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Client;

namespace MVirus.Data.Streams
{
    internal class StreamCopyUtils
    {
        public static async Task CopyStreamToAsyncWithProgress(Stream from, Stream destination, CancellationToken cancellationToken, Action<int> progress, int bufferSize = 81920)
        {
            await new StreamProgressWrapper(from, progress).CopyToAsync(destination, bufferSize);
        }

        public static async Task CopyGzipStreamToAsyncWithProgress(Stream from, Stream destination, CancellationToken cancellationToken, Action<int> progress, int bufferSize = 81920)
        {;
            var gzipStream = new GZipStream(new StreamProgressWrapper(from, progress), CompressionMode.Decompress);
            await gzipStream.CopyToAsync(destination, bufferSize);
            gzipStream.Dispose();
        }

    }
}
