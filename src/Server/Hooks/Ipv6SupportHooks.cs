using System;
using System.Net;
using HarmonyLib;
using MVirus.Config;
using MVirus.Logger;

// This path fixes 400 bad request error when a client uses ipv6 IP in hostname
// E.g. http://[::d874:252d:ef6a:1616]:24000
namespace MVirus.Server.Hooks
{
    // The game handles ':' in 'host' header as port separator
    // This conflicting with raw IPv6 addresses
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HttpListenerRequest), "FinishInitialization")]
    internal class HttpListenerRequestFinishInitializationHook
    {
        // ReSharper disable once InconsistentNaming
        static void Prefix(object __instance)
        {
            var self = __instance as HttpListenerRequest;
            var hostname = self?.Headers["host"];
            if (hostname == null)
                return;

            if (hostname.IndexOf('[') == -1 || hostname.IndexOf(']') == -1)
                return;

            var asLocalHost = "mvirus-ipv6-workaround.local:" + MVirusConfig.FilesHttpPort;
            self.Headers["host"] = asLocalHost;

            MVLog.Warning("Pathed host " + asLocalHost);
        }
    }


    [HarmonyReversePatch]
    [HarmonyPatch("EndPointListener", "SearchListener")]
    internal class EndPointListenerSearchListenerHook
    {
        // ReSharper disable once InconsistentNaming
        static void Postfix(ref HttpListener __result, ref Uri uri)
        {
            if (ServerModManager.contentServer == null || uri == null)
                return;

            MVLog.Warning("Pathed handler " );
            try
            {
                if (__result == null && uri.Port == ServerModManager.contentServer.Port)
                    __result = ServerModManager.contentServer.Listener;
            }
            catch
            {
                // ignored
            }
        }
    }
}
