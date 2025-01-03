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
            MVLog.Out("Download file: " + fileInfo.Path);

            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                netStream = await API.incomingStreamHandler.CreateFileStream(fileInfo.Path);

                fileStream = File.Open(Path.Combine(outPath, fileInfo.Path), FileMode.Create);

                await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken, progressCounter);

                MVLog.Out("Download complecte: " + fileInfo.Path);
            }
            finally
            {
                netStream?.Close();
                fileStream?.Close();
            }
        }
    }
}
