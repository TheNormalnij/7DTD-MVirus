
namespace MVirus
{
    public class Version
    {
        public static System.Version ModVersion => API.instance.Version;
        public static readonly System.Version MinimalClientVersion = System.Version.Parse("1.0.0");
    }
}
