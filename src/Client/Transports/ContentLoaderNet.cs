using MVirus.Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client.Transports
{
    public class ContentLoaderNet : ILoadingTransport
    {
        private readonly NetFileTransferManager downloadManager = new NetFileTransferManager();

        public void OnDownloadCanceled()
        {
            
        }

        public async Task DownloadFileAsync(ServerFileInfo fileInfo, string outPath, CancellationToken cancellationToken, Action<int> progressCounter)
        {
            var name = fileInfo.Path;
            Log.Out("[MVirus] Download file: " + name);

            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                PathUtils.CreatePathForDir(Path.Combine(outPath, Path.GetDirectoryName(name)));

                netStream = await downloadManager.CreateFileStream(name);

                fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken, progressCounter);

                Log.Out("[MVirus] Download complecte: " + name);
            }
            finally
            {
                netStream?.Close();
                fileStream?.Close();
            }
        }
    }
}
