using System.IO;
using MVirus.Data;
using MVirus.NetStreams;

namespace MVirus.Server.NetStreams.StreamSource
{
    public class FileStreamStaticCompressed : IStreamSource
    {
        private readonly string rootPath;

        public FileStreamStaticCompressed(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public RequestedStreamParams CreateStream(string name)
        {
            if (!PathUtils.IsSafeRelativePath(name))
                throw new NetStreamException(StreamErrorCode.NOT_FOUND);

            try
            {
                var stream = File.OpenRead(rootPath + "/" + name + ".gz");
                return new RequestedStreamParams
                {
                    stream = stream,
                    compressed = true,
                    length = stream.Length,
                    Close = () => stream.Close()
                };
            }
            catch (FileNotFoundException)
            {
                throw new NetStreamException(StreamErrorCode.NOT_FOUND);
            }
            catch (DirectoryNotFoundException)
            {
                throw new NetStreamException(StreamErrorCode.NOT_FOUND);
            }
        }
    }
}
