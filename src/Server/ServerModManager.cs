﻿using MVirus.Server.NetStreams;
using System;
using System.Collections.Generic;
using System.IO;
using MVirus.Config;
using MVirus.Logger;
using MVirus.NetPackets;
using MVirus.Server.NetStreams.StreamSource;

namespace MVirus.Server
{
    public class ServerModManager
    {
        public static ContentWebServer contentServer;
        public static OutcomingStreamHandler netTransferManager;

        public static void OnServerGameStarted() {
            try
            {
                MVLog.Out("Start delivery services");
                ContentScanner.PrepareContent();
                CreateContentDeliveryHandler();
                MVLog.Out("Services are ready");
            }
            catch (Exception e)
            {
                MVLog.Exception(e);
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
            var chain = new List<IStreamSource>();
            if (MVirusConfig.StaticFileCompression)
                chain.Add(new FileStreamStaticCompressed(ContentScanner.cachePath));

            if (MVirusConfig.CacheAllRemoteFiles)
            {
                if (MVirusConfig.ActiveFileCompression)
                    chain.Add(new FileStreamActiveCompressed(ContentScanner.cachePath));
                else
                    chain.Add(new FileStreamSource(ContentScanner.cachePath));
            } else
            {
                if (MVirusConfig.ActiveFileCompression)
                {
                    chain.Add(new FileStreamActiveCompressed(ModManager.ModsBasePath));
                    chain.Add(new FileStreamActiveCompressed(Path.GetFullPath(ModManager.ModsBasePathLegacy)));
                }
                else
                {
                    chain.Add(new FileStreamSource(ModManager.ModsBasePath));
                    chain.Add(new FileStreamSource(Path.GetFullPath(ModManager.ModsBasePathLegacy)));
                }
            }

            if (chain.Count == 1)
                return chain[0];
            else
                return new StreamSourceChain(chain.ToArray());
        }
    }
}
