using System.Reflection;

using AvatarInfection.Utilities;

using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow;

using LabFusion.Network;
using LabFusion.Player;

using static Il2CppSLZ.Marrow.Health;

namespace AvatarInfection.Patches
{
    //[HarmonyPatch(typeof(Player_Health))]
    public static class HealthPatches
    {
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Player_Health.SetHealthMode))]
        public static bool SetHealthMode(Player_Health __instance)
        {
            if (__instance == null)
                return true;

            if (__instance._rigManager != Player.RigManager)
                return true;

            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance?.IsStarted != true)
                return true;

            if (FusionPlayerExtended.MortalityOverride == null)
                return true;

            HealthMode mode = FusionPlayerExtended.MortalityOverride == true
                ? HealthMode.Mortal
                : HealthMode.Invincible;

            if (__instance.healthMode != mode)
            {
                __instance.prevHealthMode = __instance.healthMode;
                __instance.healthMode = mode;
            }
            return false;
        }

    }

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
