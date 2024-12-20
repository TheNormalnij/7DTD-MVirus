using DamienG.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Client
{
    public class CacheScanner
    {
        public static async Task<List<ServerFileInfo>> FilterLocalFiles(List<ServerFileInfo> files, string cachePath, Action<ServerFileInfo> exists)
        {
            var filteredList = new List<ServerFileInfo>();

            foreach (var fileInfo in files)
            {
                var targetPath = Path.Combine(cachePath, fileInfo.Path);

                try
                {
                    var crc32 = await Crc32.CalculateFileCrc32Async(targetPath);
                    if (crc32.Equals(fileInfo.Crc))
                        exists.Invoke(fileInfo);
                    else
                        filteredList.Add(fileInfo);
                }
                catch (FileNotFoundException _)
                {
                    filteredList.Add(fileInfo);
                }
                catch (DirectoryNotFoundException _)
                {
                    filteredList.Add(fileInfo);
                }
            }

            return filteredList;
        }
    }
}
