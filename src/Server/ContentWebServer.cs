using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Server.NetStreams;
using MVirus.Shared.NetStreams;
using MVirus.Shared;
using System.Xml.Linq;

namespace MVirus.Server
{
    public class ContentWebServer
    {
        private Thread _serverThread;
        private IStreamSource _streamSource;
        private HttpListener _listener;
        private int _port;
        private bool closing;

        public int Port
        {
            get { return _port; }
        }

        public ContentWebServer(IStreamSource streamSource, int port = 8080)
        {
            _streamSource = streamSource;
            _port = port;
            _serverThread = new Thread(Listen);
            _serverThread.Start();
        }

        public void Stop()
        {
            closing = true;
            _serverThread.Abort();
            _listener?.Stop();
        }

        private void Listen()
        {
            closing = false;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            Task.Run(ListenLoopAsync);
        }

        private async Task ListenLoopAsync()
        {
            while(!closing)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = ProcessAsync(context);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        private async Task ProcessAsync(HttpListenerContext context)
        {
            string filename = Uri.UnescapeDataString(context.Request.Url.AbsolutePath);

            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                await SendError(context.Response, HttpStatusCode.NotFound);
                return;
            }

            RequestedStreamParams streamRequest;

            try
            {
                streamRequest = _streamSource.CreateStream(filename);
            } catch (NetStreamException ex)
            {
                await SendError(context.Response, ex.ErrorCode == StreamErrorCode.NOT_FOUND ? HttpStatusCode.NotFound : HttpStatusCode.InternalServerError);
                return;
            } catch (Exception ex)
            {
                Log.Error(ex.Message);
                await SendError(context.Response, HttpStatusCode.InternalServerError);
                return;
            }

            try {
                //Adding permanent http response headers
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/octet-stream";
                context.Response.ContentLength64 = streamRequest.stream.Length;
                
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
                if (streamRequest.compressed)
                    context.Response.AddHeader("Content-Encoding", "gzip");

                await streamRequest.stream.CopyToAsync(context.Response.OutputStream);
                await context.Response.OutputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            streamRequest.stream.Close();
            context.Response.OutputStream.Close();
        }

        private async Task SendError(HttpListenerResponse response, HttpStatusCode code = HttpStatusCode.InternalServerError)
        {
            response.StatusCode = (int)code;
            await response.OutputStream.FlushAsync();
            response.OutputStream.Close();
        }

    }
}
