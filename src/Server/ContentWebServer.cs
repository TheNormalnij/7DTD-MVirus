using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Shared;

namespace MVirus.Server
{
    public class ContentWebServer
    {
        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;
        private bool closing;

        public int Port
        {
            get { return _port; }
        }

        public ContentWebServer(string path, int port = 8080)
        {
            Initialize(path, port);
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
                SendError(context.Response, HttpStatusCode.NotFound);
                return;
            }

            if (!PathUtils.IsSafeRelativePath(filename))
            {
                SendError(context.Response, HttpStatusCode.Forbidden);
                return;
            }

            filename = Path.Combine(_rootDirectory, filename);

            if (!File.Exists(filename))
            {
                SendError(context.Response, HttpStatusCode.NotFound);
                return;
            }

            FileStream input;

            try
            {
                input = File.OpenRead(filename);
            }
            catch (Exception ex)
            {
                SendError(context.Response, HttpStatusCode.InternalServerError);
                Log.Error(ex.Message);
                return;
            }

            try {
                //Adding permanent http response headers
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                await input.CopyToAsync(context.Response.OutputStream);
                await context.Response.OutputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            input.Close();
            context.Response.OutputStream.Close();
        }

        private void SendError(HttpListenerResponse response, HttpStatusCode code = HttpStatusCode.InternalServerError)
        {
            response.StatusCode = (int)code;
            response.OutputStream.Close();
        }

        private void Initialize(string path, int port)
        {
            _rootDirectory = path;
            _port = port;
            _serverThread = new Thread(Listen);
            _serverThread.Start();
        }
    }

    public class ServerStartParameters
    {
        public string Path { get; set; }
        public int Port { get; set; }
    }
}
