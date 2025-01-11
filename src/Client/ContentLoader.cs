using System.Threading;
using System;
using System.Threading.Tasks;
using MVirus.Client.Transports;
using MVirus.Shared;
using System.IO;
using System.Collections.Generic;

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
        public int MaxConcurrentConnections { get; set; } = 3;

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

            await DoDownloadLoop();

            MVLog.Out("All download tasks complected");

            State = LoadingState.COMPLECTED;
        }

        private async Task DoDownloadLoop()
        {
            var tasksArray = filesToLoad.list.ToArray();
            var index = -1;

            Task getNextTask()
            {
                index++;
                if (tasksArray.Length > index)
                    return DownloadFile(tasksArray[index]);
                return null;
            }

            List<Task> currentTasks = new List<Task>();
            for (int i = 0; i < MaxConcurrentConnections; i++)
            {
                var task = getNextTask();
                if (task == null)
                    break;

                currentTasks.Add(task);
            }

            if (currentTasks.Count == 0)
                return;

            for (; ; )
            {
                var finished = await Task.WhenAny(currentTasks);

                if (finished.IsFaulted)
                {
                    var error = finished.Exception;
                    MVLog.Exception(error);
                    MVLog.Error("DownloadFileAsync error: " + error.Message);
                    StopDownloading();
                }

                if (State == LoadingState.CANCELING)
                {
                    State = LoadingState.CANCELED;
                    MVLog.Out("Downloading canceled");
                    return;
                }

                currentTasks.Remove(finished);

                var nextTask = getNextTask();
                if (nextTask != null)
                    currentTasks.Add(nextTask);

                if (currentTasks.Count == 0)
                    break;
            }
        }

        private async Task DownloadFile(ServerFileInfo fileInfo)
        {
            PathUtils.CreatePathForDir(Path.Combine(outPath, Path.GetDirectoryName(fileInfo.Path)));
            MVLog.Out("Download file: " + fileInfo.Path);
            await transport.DownloadFileAsync(fileInfo, outPath, cancellationTokenSource.Token, ProgressCounter);
            MVLog.Out("Download complecte: " + fileInfo.Path);
        }

        private void ProgressCounter(int count)
        {
            DownloadSize -= count;
        }
    }
}
