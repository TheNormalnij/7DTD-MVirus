
namespace MVirus.ModInfo
{
    public class ServerFileInfo
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public int Crc { get; set; }

        public ServerFileInfo(string path, long size, int crc)
        {
            Path = path;
            Size = size;
            Crc = crc;
        }
    }
}
