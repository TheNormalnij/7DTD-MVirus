using MVirus.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client
{
    enum LoadingState
    {
        IDLE,
        CACHE_SCAN,
        LOADING,
        CANCELING,
        CANCELED,
        COMPLECTED
    }

    internal class ContentLoaderHttp
    {
        private List<ServerFileInfo> filesToLoad;
        private readonly string outPath;

        public LoadingState State { get; private set; }
        public RemoteHttpInfo RemoteAddr { get; private set; }

        public string CurrentFile { get; private set; }
        public long DownloadSize { get; private set; }
        public long ContentSize { get; private set; }

        private CancellationTokenSource cancellationTokenSource;
        private HttpClient Client { get; set; }
        private Task currentTask;

        public ContentLoaderHttp(RemoteHttpInfo remote, List<ServerFileInfo> files, string targetPath)
        {
            RemoteAddr = remote;
            outPath = targetPath;
            filesToLoad = files;
        }

        public void StopDownloading()
        {
            if (State != LoadingState.LOADING)
                return;

            State = LoadingState.CANCELING;
            cancellationTokenSource?.Cancel();
            Client?.CancelPendingRequests();
            Client?.Dispose();
        }

        public void Reset()
        {
            State = LoadingState.IDLE;
        }

        public async Task DownloadServerFilesAsync()
        {
            State = LoadingState.CACHE_SCAN;

            if (!IsFileListSafe())
            {
                Log.Error("[MVirus] Unsafe file path. Abort");
                State = LoadingState.CANCELED;
                return;
            }

            CalculateDownloadSize();

            try
            {
                filesToLoad = await CacheScanner.FilterLocalFiles(filesToLoad, outPath, existsFile =>
                {
                    DownloadSize -= existsFile.Size;
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Log.Error("[MVirus] Cannot handle cache. Abort");
                State = LoadingState.CANCELED;
                return;
            }

            State = LoadingState.LOADING;

            foreach (var fileInfo in filesToLoad)
            {
                await DownloadFileAsync(fileInfo.Path);

                if (State == LoadingState.CANCELING)
                {
                    State = LoadingState.CANCELED;
                    Log.Out("[MVirus] Downloading canceled");
                    return;
                }
            }

            Log.Out("[MVirus] All download tasks complected");

            State = LoadingState.COMPLECTED;
        }

        private bool IsFileListSafe()
        {
            return !filesToLoad.Exists(item => !PathUtils.IsSafeRelativePath(item.Path));
        }

        private void CalculateDownloadSize()
        {
            ContentSize = 0;
            foreach (var fileInfo in filesToLoad)
            {
                ContentSize += fileInfo.Size;
            }

            DownloadSize = ContentSize;
        }

        private async Task DownloadFileAsync(string name)
        {
            Log.Out("[MVirus] Download file: " + RemoteAddr.Url + name);
            CurrentFile = name;

            Client = new HttpClient();
            cancellationTokenSource = new CancellationTokenSource();
            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                CreateFileDir(name);

                var urlPath = Uri.EscapeDataString(name);
                netStream = await Client.GetStreamAsync(RemoteAddr.Url + urlPath);

                fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationTokenSource.Token,
                    progress: count => { DownloadSize -= count; }
                    );

                Log.Out("[MVirus] Download complecte: " + name);
            }
            catch (Exception ex)
            {
                Log.Error("[MVirus] " + ex.Message);
                StopDownloading();
            }

            cancellationTokenSource = null;
            netStream?.Close();
            fileStream?.Close();
        }

        private void CreateFileDir(string path)
        {
            var dir = Path.Combine(outPath, Path.GetDirectoryName(path));

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                SdDirectory.CreateDirectory(dir);
        }
    }
}
