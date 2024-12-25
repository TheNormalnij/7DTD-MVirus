using MVirus.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class ContentLoaderNet : ContentLoader
    {
        private NetFileTransferManager downloadManager = new NetFileTransferManager();
        public override long DownloadSize { get; protected set; }

        public ContentLoaderNet(DownloadFileQuery files, string targetPath) : base(files, targetPath)
        {
        }

        protected override void OnDownloadCanceled()
        {
            
        }

        protected override async Task DownloadFileAsync(ServerFileInfo fileInfo)
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

                Action<int> progressCounter = count => { DownloadSize -= count; };
                await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationTokenSource.Token,
                                                                    progressCounter);

                Log.Out("[MVirus] Download complecte: " + name);
            }
            catch (Exception ex)
            {
                Log.Error("[MVirus] DownloadFileAsync error: " + ex.Message);
                StopDownloading();
            }

            netStream?.Close();
            fileStream?.Close();
        }
    }
}
