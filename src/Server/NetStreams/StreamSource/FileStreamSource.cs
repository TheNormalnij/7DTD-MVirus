
using MVirus.Shared.NetStreams;
using MVirus.Shared;
using System.IO;

namespace MVirus.Server.NetStreams
{
    public class FileStreamSource : IStreamSource
    {
        private readonly string rootPath;

        public FileStreamSource(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public RequestedStreamParams CreateStream(string name)
        {
            if (!PathUtils.IsSafeRelativePath(name))
                throw new NetStreamException(StreamErrorCode.NOT_FOUND);

            try
            {
                var stream = File.OpenRead(rootPath + "/" + name);
                return new RequestedStreamParams
                {
                    stream = stream,
                    compressed = false,
                    length = stream.Length,
                    Close = () => stream.Close()
                };
            } catch (FileNotFoundException)
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
