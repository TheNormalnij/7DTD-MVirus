using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class StreamUtils
    {
        public static async Task CopyStreamToAsyncWithProgress(Stream from, Stream destination, CancellationToken cancellationToken, int bufferSize = 81920, Action<int> progress = null)
        {
            byte[] buffer = new byte[bufferSize];
            int count;
            while ((count = await from.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                progress?.Invoke(count);
            }
        }
    }
}
