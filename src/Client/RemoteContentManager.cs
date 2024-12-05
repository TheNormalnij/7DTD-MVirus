using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVirus.Client
{
    internal class RemoteContentManager
    {
        private static readonly Dictionary<string, RemoteMod> remoteMods = new Dictionary<string, RemoteMod>();

        public static ContentLoaderHttp currentLoading;
        public static RemoteHttpInfo Remote { get; set; }

        public static void RequestServerMods(List<ServerFileInfo> list)
        {
            Log.Out("[MVirus] Request server mods");
            currentLoading?.StopDownloading();

            UnloadServerMods();

            ParseRemoteMods(list);

            currentLoading = new ContentLoaderHttp(Remote, list, API.clientCachePath);
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
            {
                mod.Unload();
            }
            remoteMods.Clear();
        }

        private static void ParseRemoteMods(List<ServerFileInfo> list)
        {
            var mods = new HashSet<string>();
            foreach (var item in list)
            {
                var pos = item.Path.IndexOf('/');
                if (pos != -1)
                {
                    var resname = item.Path.Substring(0, pos);
                    mods.Add(resname);
                }
            }

            foreach (var resname in mods)
            {
                remoteMods.Add(resname, new RemoteMod(resname));
            }
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
