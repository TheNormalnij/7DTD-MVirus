using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MVirus.Client.Hooks
{
    [HarmonyPatch(typeof(WorldStaticData), nameof(WorldStaticData.WaitForConfigsFromServer))]
    internal class WorldStaticDataAllConfigsReceivedAndLoadedHook
    {
        static bool Prefix()
        {
            if (WorldStaticData.receivedConfigsHandlerCoroutine != null)
                ThreadManager.StopCoroutine(WorldStaticData.receivedConfigsHandlerCoroutine);

            WorldStaticData.receivedConfigsHandlerCoroutine = ThreadManager.StartCoroutine(HandleCo());
            return false;
        }

        private static IEnumerator HandleCo()
        {
            var nextHandler = WorldStaticData.handleReceivedConfigs();

            // Call it once to prevent race condition
            yield return nextHandler.MoveNext();

            XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Download server mods");

            XUiC_ProgressWindow.SetEscDelegate(LocalPlayerUI.primaryUI, () =>
            {
                RemoteContentManager.currentLoading?.StopDownloading();
                ConnectionManager.Instance.Disconnect();
                RemoteContentManager.UnloadServerMods();
                XUiC_ProgressWindow.SetEscDelegate(LocalPlayerUI.primaryUI, null);
            });

            var cancelText = "\n\n[FFFFFF]" + Utils.GetCancellationMessage();

            while (true)
            {
                var loader = RemoteContentManager.currentLoading;
                if (loader == null)
                    break;

                if (loader.State != LoadingState.LOADING)
                    break;

                var text = "Downloading " + (loader.DownloadSize / 1024 / 1024) + "Mb";

                XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, text + cancelText, false);

                yield return new WaitForSeconds(0.5f);
            }

            XUiC_ProgressWindow.SetEscDelegate(LocalPlayerUI.primaryUI, null);

            if (!ConnectionManager.Instance.IsConnected)
                yield break;

            if (RemoteContentManager.currentLoading?.State == LoadingState.CANCELED)
            {
                ConnectionManager.Instance.Disconnect();
                RemoteContentManager.UnloadServerMods();
                yield break;
            }

            XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Loading configs...");

            while (nextHandler.MoveNext())
                yield return nextHandler.Current;
        }
    }
}
