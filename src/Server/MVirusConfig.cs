namespace MVirus.Server
{
    public class MVirusConfig
    {
        public static ushort FilesHttpPort { get; set; } = 26901;

        public static void Load()
        {
            MVirusConfigXml.Load(new XmlFile(API.instance.Path, "config.xml"));
        }
    }
}
