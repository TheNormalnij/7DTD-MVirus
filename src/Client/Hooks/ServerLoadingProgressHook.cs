using HarmonyLib;
using MVirus.Shared.NetPackets;
using System.Collections;
using UnityEngine;

namespace MVirus.Client.Hooks
{
    [HarmonyPatch(typeof(WorldStaticData), nameof(WorldStaticData.WaitForConfigsFromServer))]
    internal class WorldStaticDataAllConfigsReceivedAndLoadedHook
    {
        static bool Prefix()
        {
            // Skip if the server has no MVirus
            var testPackage = NetPackageManager.GetPackage<NetPackageMVirusHello>().GetType();
            if (!NetPackageManager.packageClassToPackageId.ContainsKey(testPackage))
                return true;

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

            var cancelText = "\n\n[FFFFFF]" + Utils.GetCancellationMessage();

            XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, "Wait server mod list" + cancelText);

            XUiC_ProgressWindow.SetEscDelegate(LocalPlayerUI.primaryUI, () =>
            {
                RemoteContentManager.CancelLoadingProcess();
                ConnectionManager.Instance.Disconnect();
                RemoteContentManager.UnloadServerMods();
                XUiC_ProgressWindow.SetEscDelegate(LocalPlayerUI.primaryUI, null);
            });

            // Wait the progress
            while (RemoteContentManager.currentLoading == null && ConnectionManager.Instance.IsConnected)
                yield return new WaitForSeconds(0.5f);


            var loader = RemoteContentManager.currentLoading;

            while (true)
            {
                if (loader.State != LoadingState.IDLE && loader.State != LoadingState.CACHE_SCAN)
                    break;

                var text = "Scan cache " + (loader.DownloadSize / 1024 / 1024) + "Mb";

                XUiC_ProgressWindow.SetText(LocalPlayerUI.primaryUI, text + cancelText, false);

                yield return new WaitForSeconds(0.5f);
            }

            while (true)
            {
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
