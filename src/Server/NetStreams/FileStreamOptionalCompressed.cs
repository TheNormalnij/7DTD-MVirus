using MVirus.Client.NetStreams;
using MVirus.Shared;
using System.IO;

namespace MVirus.Server.NetStreams
{
    public class FileStreamOptionalCompressed : IStreamSource
    {
        private readonly string rootPath;

        public FileStreamOptionalCompressed(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public RequestedStreamParams CreateStream(string name)
        {
            if (!PathUtils.IsSafeRelativePath(name))
                throw new NetStreamException(StreamErrorCode.NOT_FOUND);

            try
            {
                var req = new RequestedStreamParams();
                var uncompressedPath = rootPath + "/" + name;

                try
                {
                    req.stream = File.OpenRead(uncompressedPath + ".gz");
                    req.compressed = true;
                } catch (FileNotFoundException)
                {
                    req.stream = File.OpenRead(uncompressedPath);
                    req.compressed = false;
                }

                return req;
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
