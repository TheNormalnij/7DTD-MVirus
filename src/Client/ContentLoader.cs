using System.Threading;
using System;
using System.Threading.Tasks;
using MVirus.Client.Transports;
using MVirus.Shared;
using System.IO;

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

    public class ContentLoader
    {
        private readonly DownloadFileQuery filesToLoad;
        private readonly string outPath;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ILoadingTransport transport;

        public LoadingState State { get; protected set; }
        public long ContentSize { get; protected set; }
        public long DownloadSize { get; protected set; }

        public ContentLoader(DownloadFileQuery _files, string targetPath, ILoadingTransport _transport)
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
                MVLog.Error("Unsafe file path. Abort");
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
                MVLog.Out("Canceled in CacheScanner.FilterLocalFiles");
                State = LoadingState.CANCELED;
                return;
            }
            catch (Exception ex)
            {
                MVLog.Exception(ex);
                MVLog.Error("Cannot handle cache. Abort");
                State = LoadingState.CANCELED;
                return;
            }

            State = LoadingState.LOADING;

            foreach (var fileInfo in filesToLoad.list)
            {
                try
                {
                    PathUtils.CreatePathForDir(Path.Combine(outPath, Path.GetDirectoryName(fileInfo.Path)));
                    Action<int> progressCounter = count => { DownloadSize -= count; };

                    MVLog.Out("Download file: " + fileInfo.Path);
                    await transport.DownloadFileAsync(fileInfo, outPath, cancellationTokenSource.Token, progressCounter);
                    MVLog.Out("Download complecte: " + fileInfo.Path);
                }
                catch (Exception ex)
                {
                    MVLog.Exception(ex);
                    MVLog.Error("DownloadFileAsync error: " + ex.Message);
                    StopDownloading();
                }

                if (State == LoadingState.CANCELING)
                {
                    State = LoadingState.CANCELED;
                    MVLog.Out("Downloading canceled");
                    return;
                }
            }

            MVLog.Out("All download tasks complected");

            State = LoadingState.COMPLECTED;
        }
    }
}
