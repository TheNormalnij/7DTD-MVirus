using MVirus.Shared.NetPackets;

namespace MVirus.Server
{
    public class MVirusConfig
    {
        public static ushort FilesHttpPort { get; set; } = 26901;
        public static RemoteFilesSource RemoteFilesSource { get; set; } = RemoteFilesSource.LOCAL_HTTP;
        public static string RemoteHttpAddr { get; set; } = "https://example.com";
        public static bool FileCompression { get; set; } = true;

        public static void Load()
        {
            MVirusConfigXml.Load(new XmlFile(API.instance.Path, "config.xml"));
        }
    }
}
