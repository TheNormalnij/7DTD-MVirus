using System.IO;

namespace MVirus.Server.NetStreams
{
    public interface IStreamSource
    {
        RequestedStreamParams CreateStream(string name);
    }

    public class RequestedStreamParams
    {
        public Stream stream;
        public bool compressed;
    }
}
