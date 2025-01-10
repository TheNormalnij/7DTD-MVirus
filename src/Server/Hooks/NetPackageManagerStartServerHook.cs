using HarmonyLib;
using MVirus.Shared.Config;
using System;
using System.Collections.Generic;

namespace MVirus.Server.Hooks
{
    // This hook removes all our packages in private local servers without mods.
    // So a player without MVirus can connect to a player with the mod.
    
    [HarmonyPatch(typeof(NetPackageManager), nameof(NetPackageManager.StartServer))]
    internal class NetPackageManagerStartServerHook
    {
        private static Dictionary<string, Type> mvirusPackages;

        static void Prefix()
        {
            if (mvirusPackages == null)
            {
                mvirusPackages = new Dictionary<string, Type>();

                foreach (var item in NetPackageManager.knownPackageTypes)
                {
                    if (item.Value.Namespace == "MVirus.Shared.NetPackets")
                        mvirusPackages.Add(item.Key, item.Value);
                }
            }

            if (MVirusConfig.IsModSharingEnabled)
            {
                foreach (var item in mvirusPackages)
                    NetPackageManager.knownPackageTypes[item.Key] = item.Value;
            } else
            {
                foreach (var item in mvirusPackages)
                    NetPackageManager.knownPackageTypes.Remove(item.Key);
            }
        }
    }
}
