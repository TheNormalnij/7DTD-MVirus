
using System.IO;

namespace MVirus.Shared
{
    internal class PathUtils
    {
        public static bool IsSafeRelativePath(string path)
        {
            path = path.Replace("\\", "/");
            return !path.Contains("../");
        }

        public static void CreatePathForDir(string dir)
        {
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                SdDirectory.CreateDirectory(dir);
        }
    }
}
