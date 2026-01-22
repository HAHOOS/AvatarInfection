using System;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow;

using Il2CppCysharp.Threading.Tasks;

using LabFusion.Network;
using LabFusion.Utilities;

using HarmonyLib;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(RigManager))]
    public static class RigManagerPatches
    {
        internal static bool Ignore { get; set; } = false;

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

                if (string.IsNullOrWhiteSpace(Infection.Instance.Config.SelectedAvatar.Value))
                    return true;

                if (barcode == new Barcode(Infection.Instance.Config.SelectedAvatar.Value))
                    return Infection.Instance.IsLocalPlayerInfected();
                else
                    return !Infection.Instance.IsLocalPlayerInfected();
            }
            catch (Exception e)
            {
                FusionModule.Logger.Error($"An unexpected error has occurred while handing SwapAvatarCrate in RigManager, exception:\n{e}");
                return true;
            }
        }

        private static bool IsBarcodeEmpty(Barcode barcode) => barcode == null || string.IsNullOrWhiteSpace(barcode.ID) || barcode.ID == Barcode.EMPTY;
    }
}