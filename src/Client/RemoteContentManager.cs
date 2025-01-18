using MVirus.Client.Transports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MVirus.Config;
using MVirus.Logger;
using MVirus.ModInfo;
using MVirus.NetPackets;

namespace MVirus.Client
{
    internal class RemoteContentManager
    {
        private static readonly SearchPathRemoteMods prefabsSearch = new SearchPathRemoteMods("Prefabs");
        private static readonly SearchPathRemoteMods prefabsImposterSearch = new SearchPathRemoteMods("Prefabs");
        private static ILoadingTransport activeTransport;
        private static bool isLoading;

        public static readonly Dictionary<string, RemoteMod> remoteMods = new Dictionary<string, RemoteMod>();

        public static ContentLoader currentLoading;

        public static void RequestServerMods(ServerModInfo[] remoteInfo)
        {
            MVLog.Out("Request server mods");
            currentLoading?.StopDownloading();
            isLoading = true;

            UnloadServerMods();

            _ = Process(remoteInfo);
        }

        public static void CancelLoadingProcess()
        {
            currentLoading?.StopDownloading();
            currentLoading = null;
            isLoading = false;
        }

        private static async Task Process(ServerModInfo[] remoteInfo)
        {
            await ParseRemoteMods(remoteInfo);

            var filesToDownload = GetAllRemoteModsFiles();
            currentLoading = new ContentLoader(filesToDownload, MVirusConfig.ClientCachePath, activeTransport);
            if (!isLoading)
            {
                MVLog.Out("Canceled loading remote mods");
                return;
            }

            await currentLoading.DownloadServerFilesAsync();
            if (currentLoading.State != LoadingState.COMPLECTED)
            {
                MVLog.Warning("Server files are not downloaded. Disconnect");
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

        private static async Task ParseRemoteMods(ServerModInfo[] list)
        {
            foreach (var remoteMod in list)
                if (!await HasLocalMod(remoteMod))
                    remoteMods.Add(remoteMod.Name, new RemoteMod(remoteMod));
        }

        private static async Task<bool> HasLocalMod(ServerModInfo modInfo)
        {
            var localMod = ModManager.GetMod(modInfo.Name);
            if (localMod == null)
                return false;

            var count = modInfo.Files.Length;

            await CacheScanner.FilterLocalFiles(modInfo.Files.ToList(), localMod.Path, CancellationToken.None, file => count--);

            return count == 0;
        }

        /// <summary>
        /// Create request for HTTP tramsport
        /// </summary>
        /// <param name="remoteHttpInfo"></param>
        public static void RequestContent(RemoteHttpInfo remoteHttpInfo)
        {
            activeTransport = new ContentLoadingTransportHttp(remoteHttpInfo);
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageMVirusHelloResponse>().Setup());
        }

        /// <summary>
        /// Create request for internal net transport
        /// </summary>
        public static void RequestContent()
        {
            activeTransport = new ContentLoadingTransportNet();
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

        private static DownloadFileQuery GetAllRemoteModsFiles()
        {
            var list = new List<ServerFileInfo>();
            foreach (var remoteMod in remoteMods.Values)
            {
                foreach (var file in remoteMod.Files)
                    list.Add(new ServerFileInfo(remoteMod.DirName + "/" + file.Path, file.Size, file.Crc));
            }
            return new DownloadFileQuery(list);
        }

        private static void LoadResources()
        {
            foreach (var mod in remoteMods.Values)
                mod.Load();
        }

        public static RemoteMod GetRemoteMod(string remoteModName)
        {
            if (remoteMods.TryGetValue(remoteModName, out var remoteMod))
                return remoteMod;

            return null;
        }
    }
}
