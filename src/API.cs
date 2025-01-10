
using HarmonyLib;
using MVirus.Client.NetStreams;
using MVirus.Server;
using MVirus.Shared.Config;

namespace MVirus
{
    public class API : IModApi
    {
        private readonly Harmony harmony = new Harmony("de.thenormalnij.mvirus");
        public static Mod instance = null;

        public static IncomingStreamHandler incomingStreamHandler = null;

        // Entrypoint
        public void InitMod(Mod _modInstance)
        {
            instance = _modInstance;

            harmony.PatchAll();

            MVirusConfig.Load();

            if (GameManager.IsDedicatedServer)
            {
                ModEvents.GameStartDone.RegisterHandler(ServerModManager.OnServerGameStarted);
                ModEvents.GameShutdown.RegisterHandler(ServerModManager.OnServerGameStopped);
            } else
            {
                incomingStreamHandler = new IncomingStreamHandler();
            }
        }
    }
}
