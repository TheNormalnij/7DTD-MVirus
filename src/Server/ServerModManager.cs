using System;

namespace MVirus.Server
{
    public class ServerModManager
    {
        private static ContentWebServer contentServer;

        public static void OnServerGameStarted() {
            try
            {
                ContentScanner.PrepareContent();

                var httpPort = GamePrefs.GetInt(EnumGamePrefs.ServerPort) + 1;
                Port = (ushort)httpPort;
                contentServer = new ContentWebServer(ContentScanner.cachePath, httpPort);
            }
            catch (Exception e)
            {
                Log.Exception(e);
                Log.Error(e.StackTrace.ToString());
            }
        }

        public static void OnServerGameStopped() { 
            contentServer?.Stop();
        }

        public static ushort Port { get; private set; }
    }
}
