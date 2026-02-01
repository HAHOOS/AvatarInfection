using System;

using AvatarInfection.Managers;

using HarmonyLib;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Network;
using LabFusion.Utilities;

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

                if (!Infection.Instance.IsStarted)
                    return true;

                if (Infection.Instance.TeamManager?.GetLocalTeam() == null)
                    return true;

                if (string.IsNullOrWhiteSpace(Infection.Instance.Config.SelectedAvatar.Value?.Barcode))
                    return true;

                if (Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.Survivors)
                {
                    return barcode != new Barcode(Infection.Instance.Config.SelectedAvatar.Value?.Barcode)
                        && barcode != new Barcode(Infection.Instance.Config.ChildrenSelectedAvatar.Value?.Barcode);
                }

                return (IsPrimary() && barcode == new Barcode(Infection.Instance.Config.SelectedAvatar.Value?.Barcode))
                    || (!IsPrimary() && barcode == new Barcode(Infection.Instance.Config.ChildrenSelectedAvatar.Value?.Barcode))
                    ? Infection.Instance.IsLocalPlayerInfected()
                    : !Infection.Instance.IsLocalPlayerInfected();
            }
            catch (Exception e)
            {
                FusionModule.Logger.Error($"An unexpected error has occurred while handing SwapAvatarCrate in RigManager, exception:\n{e}");
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(RigManager.SwapAvatarCrate))]
        public static void AvatarChangePostfix(RigManager __instance)
        {
            if (!NetworkInfo.HasServer)
                return;

            if (__instance?.IsLocalPlayer() != true)
                return;

            if (!Infection.Instance.IsStarted)
                return;

            MetadataManager.SetAvatarModId();
        }

        private static bool IsPrimary()
            => Infection.Instance.TeamManager.GetLocalTeam() != Infection.Instance.InfectedChildren || !Infection.Instance.Config.ChildrenSelectedAvatar.Enabled;

        private static bool IsBarcodeEmpty(Barcode barcode)
            => barcode == null || string.IsNullOrWhiteSpace(barcode.ID) || barcode.ID == Barcode.EMPTY;
    }
}