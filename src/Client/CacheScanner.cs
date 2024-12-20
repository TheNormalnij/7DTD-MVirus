using DamienG.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MVirus.Client
{
    public class CacheScanner
    {

        public async static Task<List<ServerFileInfo>> FilterLocalFiles(List<ServerFileInfo> files, string cachePath, Action<ServerFileInfo> exists)
        {
            if (files.Count == 0)
                return new List<ServerFileInfo>();

            return await FilterLocalFilesThisThreadAsync(files, cachePath, exists);
        }

        private static async Task<List<ServerFileInfo>> FilterLocalFilesThisThreadAsync(List<ServerFileInfo> files, string cachePath, Action<ServerFileInfo> exists)
        {

            var tasksArray = files.ToArray();
            var index = -1;

            ServerFileInfo getNextCheck()
            {
                index++;
                if (tasksArray.Length > index)
                    return tasksArray[index];
                return null;
            }

            CheckTaskInfo getNextTask()
            {
                var check = getNextCheck();
                if (check == null)
                    return null;

                Log.Warning(check.Path);
                var path = Path.Combine(cachePath, check.Path);
                return new CheckTaskInfo{ task = IsFileCached(path, check.Crc), data = check };
            }

            List<CheckTaskInfo> currentTasks = new List<CheckTaskInfo>();
            for (int i = 0; i < 3; i++)
            {
                var task = getNextTask();
                if (task == null)
                    break;

                currentTasks.Add(task);
            }

            var filteredList = new List<ServerFileInfo>();
            for (; ; )
            {
                var finished = await Task.WhenAny(currentTasks.Select( item => item.task ));

                var taskItem = currentTasks.Find(item => item.task == finished);

                if (finished.Result)
                    exists.Invoke(taskItem.data);
                else
                    filteredList.Add(taskItem.data);

                currentTasks.Remove(taskItem);

                var nextTask = getNextTask();
                if (nextTask != null)
                    currentTasks.Add(nextTask);

                if (currentTasks.Count == 0)
                    break;
            }

            return filteredList;
        }

        public static async Task<bool> IsFileCached(string targetPath, int expectedCrc)
        {
            try
            {
                var crc32 = await Crc32.CalculateFileCrc32Async(targetPath);
                return crc32.Equals(expectedCrc);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        private class CheckTaskInfo
        {
            public Task<bool> task;
            public ServerFileInfo data;
        }
    }
}
