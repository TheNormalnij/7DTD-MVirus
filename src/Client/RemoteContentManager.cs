using MVirus.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class RemoteContentManager
    {
        private static readonly Dictionary<string, RemoteMod> remoteMods = new Dictionary<string, RemoteMod>();

        public static ContentLoaderHttp currentLoading;
        public static RemoteHttpInfo Remote { get; set; }

        public static void RequestServerMods(ServerModInfo[] remoteInfo)
        {
            Log.Out("[MVirus] Request server mods");
            currentLoading?.StopDownloading();

            UnloadServerMods();

            ParseRemoteMods(remoteInfo);

            var filesToDownload = GetAllRemoteModsFiles(remoteInfo);
            currentLoading = new ContentLoaderHttp(Remote, filesToDownload, API.clientCachePath);
            _ = Process();
        }

        private static async Task Process()
        {
            await currentLoading.DownloadServerFilesAsync();
            if (GameManager.Instance.worldCreated)
                GameManager.Instance.DoSpawn();
        }

        public static void UnloadServerMods()
        {
            foreach (var mod in remoteMods.Values)
                mod.Unload();

            remoteMods.Clear();
        }

        private static void ParseRemoteMods(ServerModInfo[] list)
        {
            foreach (var remoteMod in list)
                remoteMods.Add(remoteMod.Name, new RemoteMod(remoteMod.Name));
        }

        private static List<ServerFileInfo> GetAllRemoteModsFiles(ServerModInfo[] remoteInfo)
        {
            var list = new List<ServerFileInfo>();
            foreach (var remoteMod in remoteInfo)
            {
                foreach (var file in remoteMod.Files)
                    list.Add(new ServerFileInfo(remoteMod.Name + "/" + file.Path, file.Size, file.Crc));
            }
            return list;
        }

        public static void LoadResources()
        {
            foreach (var mod in remoteMods.Values)
                mod.Load();
        }

        public static bool IsServerResource(string name)
        {
            return remoteMods.ContainsKey(name);
        }

    }
}
