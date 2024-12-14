using DamienG.Security.Cryptography;
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
        LOADING,
        CANCELING,
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
            Client?.CancelPendingRequests();
        }

        public async Task DownloadServerFilesAsync()
        {
            State = LoadingState.LOADING;

            FilterLocalFiles();
            CalculateDownloadSize();

            foreach (var fileInfo in filesToLoad)
            {
                await DownloadFileAsync(fileInfo.Path);

                if (State == LoadingState.CANCELING)
                {
                    State = LoadingState.IDLE;
                    Log.Out("[MVirus] Downloading canceled");
                    return;
                }
            }

            Log.Out("[MVirus] All download tasks complected");

            State = LoadingState.COMPLECTED;
        }

        private void FilterLocalFiles()
        {
            var filteredList = new List<ServerFileInfo>();

            foreach (var fileInfo in filesToLoad)
            {
                var targetPath = Path.Combine(outPath, fileInfo.Path);

                if (File.Exists(targetPath))
                {
                    var crc32 = Crc32.CalculateFileCrc32(targetPath);
                    if (!crc32.Equals(fileInfo.Crc))
                        filteredList.Add(fileInfo);
                }
                else
                {
                    filteredList.Add(fileInfo);
                }
            }

            filesToLoad = filteredList;
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
            try
            {
                Log.Out("[MVirus] Download file: " + RemoteAddr.Url + name);
                CurrentFile = name;

                CreateFileDir(name);

                Client = new HttpClient();
                var stream = await Client.GetStreamAsync(RemoteAddr.Url + name);

                Stream fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                await StreamUtils.CopyStreamToAsyncWithProgress(stream, fileStream, CancellationToken.None,
                    progress: count => { DownloadSize -= count; }
                    );

                fileStream.Close();
                Log.Out("[MVirus] Download complecte: " + name);
            }
            catch (Exception ex)
            {
                Log.Error("Can not download file " + name);
                Log.Exception(ex);
            }
        }

        private void CreateFileDir(string path)
        {
            var dir = Path.Combine(outPath, Path.GetDirectoryName(path));

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                SdDirectory.CreateDirectory(dir);
        }
    }
}
