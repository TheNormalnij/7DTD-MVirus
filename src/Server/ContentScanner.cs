using DamienG.Security.Cryptography;
using System.Collections.Generic;
using System.IO;

namespace MVirus.Server
{
    internal class ContentScanner
    {
        public static readonly List<ServerFileInfo> webFiles = new List<ServerFileInfo>();
        public static string cachePath;

        public static void PrepareContent()
        {
            var thisMod = API.instance;
            cachePath = thisMod.Path + "/HttpServerFiles";

            if (!SdDirectory.Exists(cachePath))
                SdDirectory.CreateDirectory(cachePath);

            foreach (var mod in ModManager.GetLoadedMods())
            {
                if (mod == thisMod || mod.Name == "0_TFP_Harmony")
                    continue;

                var modCachePath = CombineHttpPath(cachePath, mod.Name);

                if (!SdDirectory.Exists(modCachePath))
                    SdDirectory.CreateDirectory(modCachePath);

                CacheDirectory(mod.Path + "/Resources", modCachePath + "/Resources");
                CacheDirectory(mod.Path + "/UIAtlases", modCachePath + "/UIAtlases");
            }
        }

        private static void CacheDirectory(string path, string targetPath)
        {
            if (!SdDirectory.Exists(path))
                return;

            if (SdDirectory.Exists(targetPath))
                SdDirectory.Delete(targetPath, true);

            SdDirectory.CreateDirectory(targetPath);

            foreach (var dir in SdDirectory.GetDirectories(path))
            {
                CacheDirectory(dir, CombineHttpPath(targetPath, Path.GetFileName(dir)));
            }

            foreach (var file in SdDirectory.GetFiles(path))
            {
                var targetFilePath = CombineHttpPath(targetPath, Path.GetFileName(file));
                SdFile.Copy(file, targetFilePath);

                var relativePath = targetFilePath.Substring(cachePath.Length + 1);
                var size = new FileInfo(targetFilePath).Length;
                var hash = Crc32.CalculateFileCrc32(targetFilePath);

                webFiles.Add(new ServerFileInfo(relativePath, size, hash));
            }
        }

        private static string CombineHttpPath(string dir1, string dir2)
        {
            return dir1 + "/" + dir2;
        }
    }
}
