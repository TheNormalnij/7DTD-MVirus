using HarmonyLib;
using System;

namespace MVirus.Client.Hooks
{
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.PatchModPathString))]
    internal class ServersideModDirectoryPatch
    {
        static bool Prefix(ref string __result, ref string _pathString)
        {
            int startPos = _pathString.IndexOf("@modfolder(", StringComparison.OrdinalIgnoreCase);

            if (startPos >= 0)
            {
                int resnameStart = startPos + "@modfolder(".Length;
                int endPos = _pathString.IndexOf("):", StringComparison.Ordinal);

                string resourceName = _pathString.Substring(resnameStart, endPos - resnameStart);

                if (!RemoteContentManager.IsServerResource(resourceName))
                    return true;

                __result = _pathString.Substring(0, startPos) + API.instance.Path + "/Cache/" + resourceName + "/" + _pathString.Substring(endPos + 2);

                return false;
            }
            return true;
        }
    }
}
