using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class RemoteContentManager
    {
        private static readonly SearchPathRemoteMods prefabsSearch = new SearchPathRemoteMods("Prefabs");
        private static readonly SearchPathRemoteMods prefabsImposterSearch = new SearchPathRemoteMods("Prefabs");

        public static readonly Dictionary<string, RemoteMod> remoteMods = new Dictionary<string, RemoteMod>();

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
            if (currentLoading.State != LoadingState.COMPLECTED)
            {
                Log.Warning("[MVirus] Server files are not downloaded. Disconnect");
                ConnectionManager.Instance.Disconnect();
                return;
            }

            LoadResources();
            if (GameManager.Instance.worldCreated)
                GameManager.Instance.DoSpawn();

            RegisterPrefabPath();
            prefabsSearch.PopulateCache();
            prefabsImposterSearch.PopulateCache();
        }

        public static void UnloadServerMods()
        {
            foreach (var mod in remoteMods.Values)
                mod.Unload();

            remoteMods.Clear();

            UnregisterPrefabPath();
            prefabsSearch.InvalidateCache();
            prefabsImposterSearch.InvalidateCache();
        }

        private static void ParseRemoteMods(ServerModInfo[] list)
        {
            foreach (var remoteMod in list)
                remoteMods.Add(remoteMod.Name, new RemoteMod(remoteMod));
        }

        public static void RequestContent(RemoteHttpInfo remoteHttpInfo)
        {
            RemoteContentManager.Remote = remoteHttpInfo;

            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMVirusHelloResponse>().Setup());
        }

        private static void RegisterPrefabPath()
        {
            prefabsSearch.SetOwner(PathAbstractions.PrefabsSearchPaths);
            prefabsImposterSearch.SetOwner(PathAbstractions.PrefabImpostersSearchPaths);
            PathAbstractions.PrefabsSearchPaths.paths.Add(prefabsSearch);
            PathAbstractions.PrefabImpostersSearchPaths.paths.Add(prefabsImposterSearch);
        }

        private static void UnregisterPrefabPath()
        {
            PathAbstractions.PrefabsSearchPaths.paths.Remove(prefabsSearch);
            PathAbstractions.PrefabImpostersSearchPaths.paths.Remove(prefabsImposterSearch);
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
