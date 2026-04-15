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
        // TODO: account for stat overrides
        // TODO: scale mass with strength upper
        [HarmonyPriority(int.MinValue)]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Avatar.ComputeBaseStats))]
        public static void OverrideStats(Avatar __instance)
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

        [HarmonyPriority(int.MinValue)]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Avatar.ComputeMass))]
        public static void OverrideMass(Avatar __instance)
        {
            if (__instance == null || __instance.name == "[RealHeptaRig (Marrow1)]")
                return;

            var rm = __instance.GetComponentInParent<RigManager>();
            if (rm != Player.RigManager)
                return;

            if (Overrides.StrengthUpper.HasValue)
            {
                // Thank you Whaley for the math thingies!
                __instance._massTotal = (15.375f * Overrides.StrengthUpper.Value) + 62.438f;
                __instance._strengthLower = (0.6628f * Overrides.StrengthUpper.Value) + 0.4095f;
            }
        }

        public static void EnsureOverrides()
        {
            if (Infection.Instance?.IsStarted != true)
                return;

            if (Player.RigManager == null)
                return;
            var avatar = Player.RigManager.avatar;
            if (avatar == null || avatar.name == "[RealHeptaRig (Marrow1)]")
                return;
            OverrideStats(avatar);
            OverrideMass(avatar);
        }
    }
}