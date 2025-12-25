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

            if (Infection.Instance?.IsStarted != true)
                return true;

            return !Infection.Instance.IsLocalPlayerInfected();
        }
    }
}
