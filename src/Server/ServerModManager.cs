using MVirus.Shared;
using MVirus.Shared.NetPackets;
using System;

namespace MVirus.Server
{
    public class ServerModManager
    {
        private static ContentWebServer contentServer;
        public static NetFileTransferManager netTransferManager;

        public static void OnServerGameStarted() {
            try
            {
                MVirusConfig.Load();
                ContentScanner.PrepareContent();

                if (MVirusConfig.RemoteFilesSource == RemoteFilesSource.LOCAL_HTTP)
                    contentServer = new ContentWebServer(ContentScanner.cachePath, MVirusConfig.FilesHttpPort);
                else if (MVirusConfig.RemoteFilesSource == RemoteFilesSource.GAME_CONNECTION)
                    netTransferManager = new NetFileTransferManager();
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
