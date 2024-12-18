using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace MVirus.Server
{
    public class ContentWebServer
    {
        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;

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
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;

            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.OutputStream.Close();
                return;
            }


            filename = Path.Combine(_rootDirectory, filename);

            if (File.Exists(filename))
            {
                Stream input;

                try
                {
                    input = new FileStream(filename, FileMode.Open);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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

                    byte[] buffer = new byte[1024 * 32];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
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
