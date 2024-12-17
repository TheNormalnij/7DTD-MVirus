using HarmonyLib;

namespace MVirus.Client.Hooks
{
    [HarmonyPatch(typeof(ConnectionManager), nameof(ConnectionManager.Connect))]
    internal class ConnectHook
    {
        static void Prefix(ref GameServerInfo _gameServerInfo)
        {
            RemoteContentManager.currentLoading?.Reset();
        }
    }
}
