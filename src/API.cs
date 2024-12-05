
using HarmonyLib;
using MVirus.Server;
using System.IO;

namespace MVirus
{
    public class API : IModApi
    {
        private readonly Harmony harmony = new Harmony("de.thenormalnij.mvirus");
        public static Mod instance = null;

        public static string clientCachePath; 

        // Entrypoint
        public void InitMod(Mod _modInstance)
        {
            instance = _modInstance;
            clientCachePath = Path.Combine(_modInstance.Path, "Cache");

            harmony.PatchAll();

            if (GameManager.IsDedicatedServer)
            {
                ModEvents.GameStartDone.RegisterHandler(ServerModManager.OnServerGameStarted);
                ModEvents.GameShutdown.RegisterHandler(ServerModManager.OnServerGameStopped);
            }
        }
    }
}
