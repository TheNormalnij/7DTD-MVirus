using MVirus.Client.NetStreams;
using MVirus.Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client.Transports
{
    public class ContentLoadingTransportNet : ILoadingTransport
    {
        public void OnDownloadCanceled()
        {
            
        }

        public async Task DownloadFileAsync(ServerFileInfo fileInfo, string outPath, CancellationToken cancellationToken, Action<int> progressCounter)
        {
            Stream fileStream = null;
            IncomingNetStream netStream = null;
            try
            {
                netStream = await API.incomingStreamHandler.CreateFileStream(fileInfo.Path);

                fileStream = File.Open(Path.Combine(outPath, fileInfo.Path), FileMode.Create);

                if (netStream.GzipCompressed)
                    await StreamUtils.CopyGzipStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                        progressCounter);
                else
                    await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                    progressCounter);
            }
            finally
            {
                netStream?.Close();
                fileStream?.Close();
            }
        }
    }
}
