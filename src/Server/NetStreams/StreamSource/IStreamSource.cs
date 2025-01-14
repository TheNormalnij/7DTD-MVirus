using System;
using System.IO;

namespace MVirus.Server.NetStreams.StreamSource
{
    public interface IStreamSource
    {
        RequestedStreamParams CreateStream(string name);
    }

    public class RequestedStreamParams
    {
        public Stream stream;
        public bool compressed;
        public long length;
        public Action Close;
    }
}
