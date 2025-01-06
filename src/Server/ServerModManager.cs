using MVirus.Server.NetStreams;
using MVirus.Shared.NetPackets;
using System;

namespace MVirus.Server
{
    public class ServerModManager
    {
        private static ContentWebServer contentServer;
        public static OutcomingStreamHandler netTransferManager;

        public static void OnServerGameStarted() {
            try
            {
                MVirusConfig.Load();
                ContentScanner.PrepareContent();
                CreateContentDeliveryHandler();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                Log.Error(e.StackTrace.ToString());
            }
        }

        public static void OnServerGameStopped() {
            contentServer?.Stop();
            netTransferManager?.Stop();
        }

        private static void CreateContentDeliveryHandler()
        {
            if (MVirusConfig.RemoteFilesSource == RemoteFilesSource.LOCAL_HTTP)
                contentServer = new ContentWebServer(CreateFileStreamSource(), MVirusConfig.FilesHttpPort);
            else if (MVirusConfig.RemoteFilesSource == RemoteFilesSource.GAME_CONNECTION)
                netTransferManager = new OutcomingStreamHandler(CreateFileStreamSource());
        }

        private static IStreamSource CreateFileStreamSource()
        {
            if (MVirusConfig.FileCompression)
                return new FileStreamOptionalCompressed(ContentScanner.cachePath);
            else
                return new FileStreamSource(ContentScanner.cachePath);
        }
    }
}
