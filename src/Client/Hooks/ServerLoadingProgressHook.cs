using HarmonyLib;
using System;
using System.Threading.Tasks;

namespace MVirus.Client.Hooks
{
    internal class ProgressHookHelpers
    {
        public static bool IsDownlaoding() {
            var loader = RemoteContentManager.currentLoading;
            if (loader == null)
                return false;

            if (loader.State == LoadingState.LOADING)
                return true;

            return false;
        }

        public static async Task ProcessLoadingText()
        {
            XUiC_ProgressWindow.Open(LocalPlayerUI.primaryUI, null);
            XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Download server mods");

            while (true)
            {
                var loader = RemoteContentManager.currentLoading;
                if (loader == null)
                    break;

                if (loader.State != LoadingState.LOADING)
                    break;

                XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Downloading " + (loader.DownloadSize / 1024) + "Kb");

                await Task.Delay(500);
            }

            XUiC_SpawnSelectionWindow.Open(LocalPlayerUI.primaryUI, _chooseSpawnPosition: false, _enteringGame: true);
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoSpawn))]
    internal class GameManagerDoSpawnHook
    {
        static bool Prefix()
        {
            var downloading = ProgressHookHelpers.IsDownlaoding();
            if (!downloading)
                return true;

            Task.Run(ProgressHookHelpers.ProcessLoadingText);
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.RequestToSpawn))]
    internal class GameManagerRequestToSpawnHook
    {
        static bool Prefix()
        {
            var downloading = ProgressHookHelpers.IsDownlaoding();
            if (!downloading)
                return true;

            Task.Run(ProgressHookHelpers.ProcessLoadingText);
            return false;
        }
    }
}
