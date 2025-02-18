﻿
using System.IO;

namespace MVirus.Data
{
    internal class PathUtils
    {
        static readonly string[] allowedExtensions = {
            ".unity3d",
            ".jpg",
            ".png",
            ".xml",
            ".nim",
            ".tts",
            ".mesh",
            ".ins",
            ".pille",
        };

        public static bool IsAllowedModFileExtension(string path)
        {
            foreach (var ext in allowedExtensions)
            {
                if (path.EndsWith(ext))
                    return true;
            }

            return false;
        }

        public static bool IsSafeClientFilePath(string path)
        {
            return IsSafeRelativePath(path) && IsAllowedModFileExtension(path);
        }

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
