using DamienG.Security.Cryptography;
using MVirus.Shared;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace MVirus.Server
{
    internal class ContentScanner
    {
        public static readonly List<ServerModInfo> loadedMods = new List<ServerModInfo>();
        public static string cachePath;

        public static void PrepareContent()
        {
            var thisMod = API.instance;
            cachePath = thisMod.Path + "/HttpServerFiles";

            if (!SdDirectory.Exists(cachePath))
                SdDirectory.CreateDirectory(cachePath);

            foreach (var mod in ModManager.GetLoadedMods())
            {
                if (mod == thisMod || mod.Name == "TFP_Harmony")
                    continue;

                var modDirectoryName = Path.GetFileName(mod.Path);
                var modCachePath = CombineHttpPath(cachePath, modDirectoryName);

                if (!SdDirectory.Exists(modCachePath))
                    SdDirectory.CreateDirectory(modCachePath);

                var files = new List<ServerFileInfo>();

                var startCut = mod.Path.Length + 1;
                CacheDirectory(files, startCut, mod.Path, modCachePath);

                var modInfo = new ServerModInfo { Name = mod.Name, DirName = modDirectoryName, Files = files.ToArray() };
                loadedMods.Add(modInfo);
            }
        }

        private static void CacheDirectory(List<ServerFileInfo> outList, int startCut, string path, string targetPath)
        {
            if (!SdDirectory.Exists(path))
                return;

            if (SdDirectory.Exists(targetPath))
                SdDirectory.Delete(targetPath, true);

            if (MVirusConfig.CacheAllRemoteFiles)
                SdDirectory.CreateDirectory(targetPath);

            foreach (var dir in SdDirectory.GetDirectories(path))
            {
                CacheDirectory(outList, startCut, dir, CombineHttpPath(targetPath, Path.GetFileName(dir)));
            }

            foreach (var file in SdDirectory.GetFiles(path))
            {
                if (!PathUtils.IsAllowedModFileExtension(file))
                    continue;

                var targetFilePath = CombineHttpPath(targetPath, Path.GetFileName(file));
                var outPath = CacheFile(file, targetFilePath);

                var relativePath = file.Substring(startCut).Replace('\\', '/');
                var size = new FileInfo(outPath).Length;
                var hash = Crc32.CalculateFileCrc32(file);

                outList.Add(new ServerFileInfo(relativePath, size, hash));
            }
        }

        private static string CacheFile(string filePath, string targetFilePath)
        {
            if (IsFileShouldBeCompressed(filePath))
            {
                try
                {
                    var outPath = targetFilePath + ".gz";
                    CompressFile(filePath, outPath);
                    return outPath;
                } catch
                {
                    if (MVirusConfig.CacheAllRemoteFiles)
                    {
                        SdFile.Copy(filePath, targetFilePath);
                        return targetFilePath;
                    }
                }
            }
            else
            {
                if (MVirusConfig.CacheAllRemoteFiles)
                {
                    SdFile.Copy(filePath, targetFilePath);
                    return targetFilePath;
                }
            }
            return filePath;
        }

        private static void CompressFile(string filePath, string targetFilePath)
        {
            FileStream sourceStream = null;
            FileStream targetStream = null;
            GZipStream gzipStream = null;
            try
            {
                sourceStream = File.OpenRead(filePath);
                targetStream = File.Create(targetFilePath);
                gzipStream = new GZipStream(targetStream, CompressionMode.Compress);
                sourceStream.CopyTo(gzipStream);
            }
            finally
            {
                gzipStream?.Dispose();
                sourceStream?.Dispose();
                targetStream?.Dispose();
            }
        }

        private static bool IsFileShouldBeCompressed(string filePath)
        {
            if (!MVirusConfig.StaticFileCompression)
                return false;

            if (filePath.EndsWith(".png"))
                return false;

            if (new FileInfo(filePath).Length < 200)
                return false;

            return true;
        }

        private static string CombineHttpPath(string dir1, string dir2)
        {
            return dir1 + "/" + dir2;
        }
    }
}
