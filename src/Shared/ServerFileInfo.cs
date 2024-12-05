
namespace MVirus
{
    public class ServerFileInfo
    {
        public string Path { get; }
        public long Size { get; }
        public int Crc { get; }

        public ServerFileInfo(string path, long size, int crc)
        {
            Path = path;
            Size = size;
            Crc = crc;
        }
    }
}
