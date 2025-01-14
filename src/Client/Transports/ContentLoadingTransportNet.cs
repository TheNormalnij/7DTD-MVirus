using MVirus.Client.NetStreams;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Data.Streams;
using MVirus.ModInfo;

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
                {
                    await StreamCopyUtils.CopyGzipStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                        progressCounter);

                    // Server returns full file size when active compression is used.
                    if (netStream.TotalCount < fileInfo.Size)
                        progressCounter.Invoke((int)(fileInfo.Size - netStream.TotalCount));
                }
                else
                    await StreamCopyUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
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
