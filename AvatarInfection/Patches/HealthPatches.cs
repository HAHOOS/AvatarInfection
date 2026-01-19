using System.Reflection;

using AvatarInfection.Utilities;

using BoneLib;

using HarmonyLib;

using LabFusion.Network;
using LabFusion.Player;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(LocalHealth))]
    public static class LocalHealthPatches
    {
        public static MethodBase TargetMethod()
        {
            var type = typeof(LocalHealth);
            return AccessTools.Method(type, "OnOverrideHealth");
        }

        public static bool Prefix()
        {
            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance?.IsStarted != true)
                return true;

            return false;
        }
    }
}