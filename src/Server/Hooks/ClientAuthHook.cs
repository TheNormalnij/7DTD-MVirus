﻿using HarmonyLib;
using MVirus.Shared.NetPackets;

namespace MVirus.Server.Hooks
{
    [HarmonyPatch(typeof(AuthFinalizer), nameof(AuthFinalizer.ReplyReceived))]
    internal class ServersideModDirectoryPatch
    {
        static void Prefix(ref ClientInfo _cInfo)
        {
            if (!GameManager.IsDedicatedServer)
                return;

            _cInfo.SendPackage(NetPackageManager.GetPackage<NetPackageMVirusHello>()
                .Setup(
                    MVirusConfig.RemoteFilesSource,
                    MVirusConfig.FilesHttpPort,
                    MVirusConfig.RemoteHttpAddr)
                );
        }
    }
}
