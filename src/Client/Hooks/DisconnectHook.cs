using HarmonyLib;

namespace MVirus.Client.Hooks
{
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Disconnect))]
    internal class DisconnectHook
    {
        static void Prefix()
        {
            RemoteContentManager.UnloadServerMods();
        }
    }
}
