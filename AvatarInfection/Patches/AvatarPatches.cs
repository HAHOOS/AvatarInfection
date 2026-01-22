using AvatarInfection.Utilities;

using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.VRMK;

using static Il2CppSLZ.Marrow.Health;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(Avatar))]
    public static class AvatarPatches
    {
        [HarmonyPriority(int.MinValue)]
        [HarmonyPatch(nameof(Avatar.ComputeBaseStats))]
        public static void Postfix(Avatar __instance)
        {
            if (__instance == null || __instance.name == "[RealHeptaRig (Marrow1)]")
                return;

            var rm = __instance.GetComponentInParent<RigManager>();
            if (rm != Player.RigManager)
                return;

            if (Overrides.Speed.HasValue)
                __instance._speed = Overrides.Speed.Value;

            if (Overrides.Agility.HasValue)
                __instance._agility = Overrides.Agility.Value;

            if (Overrides.Vitality.HasValue)
                __instance._vitality = Overrides.Vitality.Value;

            if (Overrides.Mortality.HasValue)
                rm.health.healthMode = (Overrides.Mortality.Value ? HealthMode.Mortal : HealthMode.Invincible);

            if (Overrides.StrengthUpper.HasValue)
            {
                __instance._strengthUpper = Overrides.StrengthUpper.Value;
                __instance._strengthGrip = Overrides.StrengthUpper.Value;
            }
        }
    }
}