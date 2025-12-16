using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;

using LabFusion.Network;
using LabFusion.SDK.Gamemodes;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(PullCordDevice))]
    public static class PullCordDevicePatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PullCordDevice.EnableBall))]
        public static bool OnBall(PullCordDevice __instance)
        {
            if (__instance == null)
                return true;

            if (__instance.GetComponentInParent<RigManager>() != Player.RigManager)
                return true;

            if (!NetworkInfo.HasServer)
                return true;

            if (!Infection.Instance.IsStarted)
                return true;

            return !Infection.Instance.IsLocalPlayerInfected();
        }
    }
}
