using System.Threading;
using System;
using System.Threading.Tasks;
using MVirus.Client.Transports;

namespace MVirus.Client
{
    public enum LoadingState
    {
        IDLE,
        CACHE_SCAN,
        LOADING,
        CANCELING,
        CANCELED,
        COMPLECTED
    }

    public abstract class ContentLoader
    {
        private DownloadFileQuery filesToLoad;
        private string outPath;
        private CancellationTokenSource cancellationTokenSource;
        private ILoadingTransport transport;

        public LoadingState State { get; protected set; }
        public long ContentSize { get; protected set; }
        public abstract long DownloadSize { get; protected set; }

        protected ContentLoader(DownloadFileQuery _files, string targetPath, ILoadingTransport _transport)
        {
            filesToLoad = _files;
            outPath = targetPath;
            transport = _transport;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void StopDownloading()
        {
            if (State > LoadingState.LOADING)
                return;

            State = LoadingState.CANCELING;
            cancellationTokenSource.Cancel();
            transport.OnDownloadCanceled();
        }

        public async Task DownloadServerFilesAsync()
        {
            State = LoadingState.CACHE_SCAN;

            if (!filesToLoad.IsFileListSafe())
            {
                Log.Error("[MVirus] Unsafe file path. Abort");
                State = LoadingState.CANCELED;
                return;
            }

            DownloadSize = filesToLoad.CalculateDownloadSize();
            ContentSize = DownloadSize;

            try
            {
                await filesToLoad.FilterLocalFiles(outPath, cancellationTokenSource.Token, existsFile =>
                {
                    DownloadSize -= existsFile.Size;
                });
            }
            catch (OperationCanceledException)
            {
                // Doesn't work. Why?
                Log.Out("[MVirus] Canceled in CacheScanner.FilterLocalFiles");
                State = LoadingState.CANCELED;
                return;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Log.Error("[MVirus] Cannot handle cache. Abort");
                State = LoadingState.CANCELED;
                return;
            }

            State = LoadingState.LOADING;

            foreach (var fileInfo in filesToLoad.list)
            {
                await DownloadFileAsync(fileInfo);

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
    }
}
