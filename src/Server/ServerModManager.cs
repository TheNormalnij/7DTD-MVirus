using System;

namespace MVirus.Server
{
    public class ServerModManager
    {
        private static ContentWebServer contentServer;

        public static void OnServerGameStarted() {
            try
            {
                MVirusConfig.Load();
                ContentScanner.PrepareContent();

                contentServer = new ContentWebServer(ContentScanner.cachePath, MVirusConfig.FilesHttpPort);
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
    }
}
