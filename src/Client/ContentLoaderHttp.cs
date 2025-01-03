using MVirus.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private long _downloadSize = 0;

        public string CurrentFile { get; private set; }
        public long DownloadSize { get { return _downloadSize; } }
        public long ContentSize { get; private set; }

        private CancellationTokenSource cancellationTokenSource;
        private HttpClient Client { get; set; }
        private Task currentTask;

        public ContentLoaderHttp(RemoteHttpInfo remote, List<ServerFileInfo> files, string targetPath)
        {
            RemoteAddr = remote;
            outPath = targetPath;
            filesToLoad = files;
            Client = new HttpClient();
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void StopDownloading()
        {
            if (State > LoadingState.LOADING)
                return;

            State = LoadingState.CANCELING;
            cancellationTokenSource.Cancel();
            Client.CancelPendingRequests();
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
                filesToLoad = await CacheScanner.FilterLocalFiles(filesToLoad, outPath, cancellationTokenSource.Token, existsFile =>
                {
                    Interlocked.Add(ref _downloadSize, -existsFile.Size);
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
            return !filesToLoad.Exists(item => !PathUtils.IsSafeClientFilePath(item.Path));
        }

        private void CalculateDownloadSize()
        {
            ContentSize = 0;
            foreach (var fileInfo in filesToLoad)
            {
                ContentSize += fileInfo.Size;
            }

            _downloadSize = ContentSize;
        }

        private async Task DownloadFileAsync(string name)
        {
            Log.Out("[MVirus] Download file: " + RemoteAddr.Url + name);
            CurrentFile = name;

            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                CreateFileDir(name);

                var urlPath = RemoteAddr.Url + Uri.EscapeDataString(name);

                var request = new HttpRequestMessage(HttpMethod.Get, urlPath);

                var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                netStream = await response.Content.ReadAsStreamAsync();

                fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                Action<int> progressCounter = count => { _downloadSize -= count; };

                if (response.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip")
                    await StreamUtils.CopyGzipStreamToAsyncWithProgress(netStream, fileStream, cancellationTokenSource.Token,
                                                                        progressCounter);
                else
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

        private void CreateFileDir(string path)
        {
            var dir = Path.Combine(outPath, Path.GetDirectoryName(path));

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                SdDirectory.CreateDirectory(dir);
        }
    }
}
