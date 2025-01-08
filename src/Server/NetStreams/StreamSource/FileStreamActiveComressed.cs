
using MVirus.Shared.NetStreams;
using MVirus.Shared;
using System.IO;
using System.IO.Compression;
using MVirus.Shared.Compression;

namespace MVirus.Server.NetStreams
{
    public class FileStreamActiveCompressed : IStreamSource
    {
        private readonly string rootPath;

        public FileStreamActiveCompressed(string rootPath)
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

                if (CanBeCompressed(stream))
                {
                    var gzipStream = new GZipStreamReversed(stream);
                    return new RequestedStreamParams
                    {
                        stream = gzipStream,
                        compressed = true,
                        length = stream.Length,
                        Close = () =>
                        {
                            gzipStream.Close();
                            stream.Close();
                        }
                    };

                } else
                {
                    return new RequestedStreamParams
                    {
                        stream = stream,
                        compressed = false,
                        length = stream.Length,
                        Close = () => stream.Close()
                    };
                }
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

        private bool CanBeCompressed(FileStream stream)
        {
            if (stream.Name.EndsWith(".png"))
                return false;

            if (stream.Length < 200)
                return false;

            return true;
        }
    }
}
