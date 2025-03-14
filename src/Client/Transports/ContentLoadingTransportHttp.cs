﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Data.Streams;
using MVirus.ModInfo;

namespace MVirus.Client.Transports
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
            Stream fileStream = null;
            Stream netStream = null;
            try
            {
                var urlPath = RemoteAddr.Url + Uri.EscapeDataString(fileInfo.Path);

                var request = new HttpRequestMessage(HttpMethod.Get, urlPath);

                var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                netStream = await response.Content.ReadAsStreamAsync();

                fileStream = File.Open(Path.Combine(outPath, fileInfo.Path), FileMode.Create);

                if (response.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip")
                {
                    await StreamCopyUtils.CopyGzipStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                        progressCounter);

                    // Server returns full file size when active compression is used.
                    if (fileStream.Length < fileInfo.Size)
                        progressCounter.Invoke((int)(fileInfo.Size - fileStream.Length));
                }
                else
                    await StreamCopyUtils.CopyStreamToAsyncWithProgress(netStream, fileStream, cancellationToken,
                                                                    progressCounter);
            }
            finally
            {
                netStream?.Close();
                fileStream?.Close();
            }
        }
    }
}
