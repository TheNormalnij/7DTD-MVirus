namespace MVirus.Client
{
    public class RemoteHttpInfo
    {
        public string Url { get; private set; }

        public RemoteHttpInfo(string ip, ushort port)
        {
            Url = "http://" + ip + ":" + port + "/";
        }

        public RemoteHttpInfo(string url) {
            if (!url.EndsWith("/"))
                url += "/";
            Url = url;
        }
    }
}
