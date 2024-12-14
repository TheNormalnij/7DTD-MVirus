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

            while (true)
            {
                var loader = RemoteContentManager.currentLoading;
                if (loader == null)
                    break;

                if (loader.State != LoadingState.LOADING)
                    break;

                XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Downloading " + (loader.DownloadSize / 1024 / 1024) + "Mb");

                yield return new WaitForSeconds(0.5f);
            }

            XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Loading configs...");

            while (nextHandler.MoveNext())
                yield return nextHandler.Current;
        }
    }
}
