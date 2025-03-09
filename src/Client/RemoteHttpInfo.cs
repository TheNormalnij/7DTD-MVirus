namespace MVirus.Client
{
    public class RemoteHttpInfo
    {
        public string Url { get; private set; }

        public RemoteHttpInfo(string ip, ushort port)
        {
            if (IsIpv4(ip))
            {
                // IPv4
                Url = $"http://{ip}:{port}/";
            }
            else
            {
                // IPv6
                Url = $"http://[{ip}]:{port}/";
            }
        }

        public RemoteHttpInfo(string url) {
            if (!url.EndsWith("/"))
                url += "/";
            Url = url;
        }

        private static bool IsIpv4(string ip)
        {
            return ip.IndexOf(':') == -1;
        }
    }
}
