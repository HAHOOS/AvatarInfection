using System;

using AvatarInfection.Managers;

using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Network;
using LabFusion.Utilities;

using static AvatarInfection.Infection;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(RigManager))]
    public static class RigManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(RigManager.SwapAvatarCrate))]
        public static bool AvatarChangeOverride(RigManager __instance, Barcode barcode, ref UniTask<bool> __result)
        {
            try
            {
                __result = new UniTask<bool>(true);

                if (IsBarcodeEmpty(barcode))
                    return true;

                if (!NetworkInfo.HasServer)
                    return true;

                if (__instance?.IsLocalPlayer() != true)
                    return true;

                if (!Instance.IsStarted)
                    return true;

                if (Instance.TeamManager?.GetLocalTeam() == null)
                    return true;

                if (string.IsNullOrWhiteSpace(Instance.Config.SelectedAvatar.Value?.Barcode))
                    return true;

                if (Instance.TeamManager.GetLocalTeam() == Instance.Survivors)
                    return barcode != Instance.Config.SelectedAvatar.AsBarcode() && barcode != Instance.Config.ChildrenSelectedAvatar.AsBarcode();

                return barcode?.ID == GetOverrideBarcode() ? Instance.IsLocalPlayerInfected() : !Instance.IsLocalPlayerInfected();
            }
            catch (Exception e)
            {
                FusionModule.Logger.Error($"An unexpected error has occurred while handing SwapAvatarCrate in RigManager, exception:\n{e}");
                return true;
            }
        }

        private static string GetOverrideBarcode()
        {
            if (IsPrimary())
                return Instance.Config.SelectedAvatar.Value?.Barcode;
            else
                return Instance.Config.ChildrenSelectedAvatar.Value?.Barcode;
        }

        [HarmonyPostfix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(RigManager.SwapAvatarCrate))]
        [HarmonyPatch(nameof(RigManager.SwapAvatar))]
        [HarmonyPatch(nameof(RigManager.SwitchAvatar))]
        public static void AvatarChangePostfix(RigManager __instance)
        {
            if (!NetworkInfo.HasServer)
                return;

            if (__instance?.IsLocalPlayer() != true)
                return;

            if (!Instance.IsStarted)
                return;

            MetadataManager.SetAvatarModId();
        }

        private static bool IsPrimary()
            => Instance.TeamManager.GetLocalTeam() != Instance.InfectedChildren || !Instance.Config.ChildrenSelectedAvatar.Enabled;

        private static bool IsBarcodeEmpty(Barcode barcode)
            => barcode == null || string.IsNullOrWhiteSpace(barcode.ID) || barcode.ID == Barcode.EMPTY;
    }
}