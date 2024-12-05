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
            Url = url;
        }
    }
}
