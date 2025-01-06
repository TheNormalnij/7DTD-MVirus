
namespace MVirus.Shared
{
    public class ServerModInfo
    {
        public string Name { get; set; }
        public ServerFileInfo[] Files { get; set; }

        public ServerModInfo(string name, ServerFileInfo[] files)
        {
            Name = name;
            Files = files;
        }
    }
}
