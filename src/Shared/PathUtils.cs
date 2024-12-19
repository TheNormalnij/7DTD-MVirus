
namespace MVirus.Shared
{
    internal class PathUtils
    {
        public static bool IsSafeRelativePath(string path)
        {
            path = path.Replace("\\", "/");
            return !path.Contains("../");
        }
    }
}
