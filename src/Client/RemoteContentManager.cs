using MVirus.Client.Transports;
using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class RemoteContentManager
    {
        private static readonly SearchPathRemoteMods prefabsSearch = new SearchPathRemoteMods("Prefabs");
        private static readonly SearchPathRemoteMods prefabsImposterSearch = new SearchPathRemoteMods("Prefabs");
        private static ILoadingTransport activeTransport;

        public static readonly Dictionary<string, RemoteMod> remoteMods = new Dictionary<string, RemoteMod>();

        public static ContentLoader currentLoading;

        public static void RequestServerMods(ServerModInfo[] remoteInfo)
        {
            Log.Out("[MVirus] Request server mods");
            currentLoading?.StopDownloading();

            UnloadServerMods();

            ParseRemoteMods(remoteInfo);

            var filesToDownload = GetAllRemoteModsFiles(remoteInfo);
            currentLoading = new ContentLoader(filesToDownload, API.clientCachePath, activeTransport);
            _ = Process();
        }

        public static void CancelLoadingProcess()
        {
            currentLoading?.StopDownloading();
            currentLoading = null;
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
        /// Create request for interenal net transport
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

        private static DownloadFileQuery GetAllRemoteModsFiles(ServerModInfo[] remoteInfo)
        {
            var list = new List<ServerFileInfo>();
            foreach (var remoteMod in remoteInfo)
            {
                foreach (var file in remoteMod.Files)
                    list.Add(new ServerFileInfo(remoteMod.DirName + "/" + file.Path, file.Size, file.Crc));
            }
            return new DownloadFileQuery(list);
        }

        public static void LoadResources()
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
