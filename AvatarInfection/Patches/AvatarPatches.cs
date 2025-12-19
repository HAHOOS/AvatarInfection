using AvatarInfection.Utilities;

using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.VRMK;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(Avatar))]
    public static class AvatarPatches
    {
        [HarmonyPatch(nameof(Avatar.ComputeBaseStats))]
        public static void Postfix(Avatar __instance)
        {
            if (__instance == null || __instance.name == "[RealHeptaRig (Marrow1)]")
                return;

            if (__instance.GetComponentInParent<RigManager>() != Player.RigManager)
                return;

            if (FusionPlayerExtended.SpeedOverride != null)
                __instance._speed = (float)FusionPlayerExtended.SpeedOverride;

            if (FusionPlayerExtended.AgilityOverride != null)
                __instance._agility = (float)FusionPlayerExtended.AgilityOverride;

            if (FusionPlayerExtended.StrengthUpperOverride != null)
            {
                __instance._strengthUpper = (float)FusionPlayerExtended.StrengthUpperOverride;
                __instance._strengthGrip = (float)FusionPlayerExtended.StrengthUpperOverride;
            }
        }
    }
}