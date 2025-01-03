
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
                return new RequestedStreamParams
                {
                    stream = File.OpenRead(rootPath + "/" + name),
                    compressed = false,
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
