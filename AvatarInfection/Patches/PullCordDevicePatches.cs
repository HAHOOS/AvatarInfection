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

            if (!IsGamemodeActive())
                return true;

            return !IsInfected();
        }

        private static bool IsInfected() => Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.Infected
            || Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.InfectedChildren;

        private static bool IsGamemodeActive()
        {
            if (!GamemodeManager.IsGamemodeStarted)
                return false;

            if (GamemodeManager.ActiveGamemode == null
                || GamemodeManager.ActiveGamemode.Barcode != Constants.Defaults.Barcode)
            {
                return false;
            }

            return true;
        }
    }
}
