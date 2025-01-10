
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

            if (MVirusConfig.IsModSharingEnabled)
            {
                ModEvents.GameStartDone.RegisterHandler(() => {
                    if (!ConnectionManager.Instance.IsClient)
                        ServerModManager.OnServerGameStarted();
                });
                // This method should handle all states
                ModEvents.GameShutdown.RegisterHandler(ServerModManager.OnServerGameStopped);
            }

            if (!GameManager.IsDedicatedServer)
                incomingStreamHandler = new IncomingStreamHandler();
        }
    }
}
