using MVirus.Shared;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class ContentLoaderHttp : ContentLoader
    {
        private RemoteHttpInfo RemoteAddr { get; set; }
        private HttpClient Client { get; set; }
        public override long DownloadSize { get; protected set; }

        public ContentLoaderHttp(RemoteHttpInfo remote, DownloadFileQuery files, string targetPath) : base(files, targetPath)
        {
            RemoteAddr = remote;
            Client = new HttpClient();
        }

        protected override void OnDownloadCanceled()
        {
            Client.CancelPendingRequests();
        }

        protected override async Task DownloadFileAsync(ServerFileInfo fileInfo)
        {
            var name = fileInfo.Path;
            Log.Out("[MVirus] Download file: " + RemoteAddr.Url + name);

            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                PathUtils.CreatePathForDir(Path.Combine(outPath, Path.GetDirectoryName(name)));

                var urlPath = RemoteAddr.Url + Uri.EscapeDataString(name);

                var request = new HttpRequestMessage(HttpMethod.Get, urlPath);

                var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                netStream = await response.Content.ReadAsStreamAsync();

                fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                Action<int> progressCounter = count => { DownloadSize -= count; };

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
    }
}
