using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;

using LabFusion.Network;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(PullCordDevice))]
    public static class PullCordDevicePatches
    {
        // HACK: this will still display the ball, but prevents anything from happening when grabbing it. Should change it later probably
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PullCordDevice.OnBallGripAttached))]
        [HarmonyPatch(nameof(PullCordDevice.OnBallGripDetached))]
        [HarmonyPatch(nameof(PullCordDevice.OnBallGripUpdate))]
        public static bool OnBall(PullCordDevice __instance)
        {
            if (__instance == null)
                return true;

            if (__instance.GetComponentInParent<RigManager>() != Player.RigManager)
                return true;

            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance?.IsStarted != true)
                return true;

            return !Infection.Instance.IsLocalPlayerInfected();
        }
    }
}