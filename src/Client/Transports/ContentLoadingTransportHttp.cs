using MVirus.Client.Transports;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MVirus.Client
{
    public class ContentLoadingTransportHttp : ILoadingTransport
    {
        private RemoteHttpInfo RemoteAddr { get; set; }
        private HttpClient Client { get; set; }

        public ContentLoadingTransportHttp(RemoteHttpInfo remote)
        {
            RemoteAddr = remote;
            Client = new HttpClient();
        }

        public void OnDownloadCanceled()
        {
            Client.CancelPendingRequests();
        }

        public async Task DownloadFileAsync(ServerFileInfo fileInfo, string outPath, CancellationToken cancellationToken, Action<int> progressCounter)
        {
            var name = fileInfo.Path;
            Log.Out("[MVirus] Download file: " + RemoteAddr.Url + name);

            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                var urlPath = RemoteAddr.Url + Uri.EscapeDataString(name);

                var request = new HttpRequestMessage(HttpMethod.Get, urlPath);

                var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                netStream = await response.Content.ReadAsStreamAsync();

                fileStream = File.Open(Path.Combine(outPath, name), FileMode.Create);

                if (response.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip")
                    await StreamUtils.CopyGzipStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                        progressCounter);
                else
                    await StreamUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                    progressCounter);

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
