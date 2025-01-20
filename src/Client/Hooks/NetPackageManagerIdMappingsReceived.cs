using HarmonyLib;
using MVirus.Logger;

namespace MVirus.Client.Hooks
{
    // The hook fixes protocol error with new versions of the mod

    [HarmonyPatch(typeof(NetPackageManager), nameof(NetPackageManager.IdMappingsReceived))]
    internal class NetPackageManagerIdMappingsReceived
    {
        static void Prefix(ref string[] _mappings)
        {
            for (int id = 0; id < _mappings.Length; ++id)
            {
                var package = _mappings[id];
                if (!NetPackageManager.knownPackageTypes.TryGetValue(package, out _) && package.StartsWith("NetPackageMVirus"))
                {
                    _mappings[id] = "NetPackageMVirusDummy";
                    MVLog.Warning($"Unknown package: {package}. Replaced with dummy");
                }
            }

            if (_mappings.Length > NetPackageManager.KnownPackageCount)
                ResizePackageMapping(_mappings.Length);
        }

        private static void ResizePackageMapping(int newSize)
        {
            NetPackageManager.packageIdToClass = new System.Type[newSize];
            NetPackageManager.packageIdToPackageInformation = new NetPackageManager.IPackageInformation[newSize];
            NetPackageManager.AddPackageMapping(0, NetPackageManager.packageIdsType);
        }
    }
}
