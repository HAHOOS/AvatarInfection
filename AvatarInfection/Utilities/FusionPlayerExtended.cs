using BoneLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Data;
using LabFusion.Downloading;
using LabFusion.Marrow;
using LabFusion.Player;
using LabFusion.Preferences.Client;
using LabFusion.RPC;

using UnityEngine;

namespace AvatarInfection.Utilities
{
    public static class FusionPlayerExtended
    {
        public static float? SpeedOverride { get; private set; }

        public static float? JumpPowerOverride { get; private set; }

        public static float? AgilityOverride { get; private set; }

        public static float? StrengthUpperOverride { get; private set; }

        // Avatar override calls everything in FusionPlayer, but restores the old avatar
        public static string AvatarOverride { get; private set; }

        public static string LastAvatar { get; private set; }

        internal static void SetOverrides(float? jumpPower, float? speed, float? agility, float? strengthUpper)
        {
            SpeedOverride = speed;
            JumpPowerOverride = jumpPower;
            AgilityOverride = agility;
            StrengthUpperOverride = strengthUpper;

            var rm = Player.RigManager;
            var avatar = rm?._avatar;

            if (avatar != null)
            {
                bool changed = false;

                if (SetOverrideValue(AgilityOverride, avatar._agility, ref changed, out float res))
                    avatar._agility = res;

                if (SetOverrideValue(JumpPowerOverride, avatar._strengthLower, ref changed, out float res2))
                    avatar._strengthLower = res2;

                if (SetOverrideValue(SpeedOverride, avatar._speed, ref changed, out float res3))
                    avatar._speed = res3;

                if (SetOverrideValue(StrengthUpperOverride, avatar._strengthUpper, ref changed, out float res4))
                {
                    avatar._strengthUpper = res4;
                    avatar._strengthGrip = res4;
                }

                if (changed)
                    rm.SwapAvatarCrate(rm.AvatarCrate.Barcode);

            }
        }

        private static bool SetOverrideValue(float? _override, float? actual, ref bool changed, out float res)
        {
            if (_override.HasValue && !actual.Equals(AgilityOverride.Value))
            {
                changed = true;
                res = _override.Value;
                return true;
            }
            res = -1f;
            return false;
        }


        internal static void ClearAllOverrides()
        {
            // This is the worst way to do it, but I don't feel like overcomplicating this.
            // This only exists so that you don't change your avatar unnecessarily which under certain circumstances causes a lot of lags for a few seconds
            // Aka when 10 players change avatar to clear overrides
            if (AgilityOverride == null &&
                SpeedOverride == null &&
                StrengthUpperOverride == null &&
                JumpPowerOverride == null &&
                SpeedOverride == null)
            {
                return;
            }

            AgilityOverride = null;
            SpeedOverride = null;
            StrengthUpperOverride = null;
            JumpPowerOverride = null;
            SpeedOverride = null;
            if (Player.RigManager != null)
            {
                var rm = Player.RigManager;
                if (rm.AvatarCrate != null)
                    rm.SwapAvatarCrate(rm.AvatarCrate.Barcode);
            }
        }

        public static void SetAvatarOverride(string barcode)
        {
            bool wasEmpty = string.IsNullOrEmpty(AvatarOverride);
            if (Player.RigManager != null && AssetWarehouse.ready && wasEmpty)
                LastAvatar = Player.RigManager.AvatarCrate.Barcode.ID ?? CommonBarcodes.Avatars.PolyBlank;

            AvatarOverride = barcode;
            SwapAvatar(barcode);
        }

        public static void ClearAvatarOverride()
        {
            AvatarOverride = null;
            if (Player.RigManager != null && !string.IsNullOrWhiteSpace(LastAvatar) && AssetWarehouse.ready)
            {
                SwapAvatar(LastAvatar);
                LastAvatar = null;
            }
        }

        private static void ForceChange(string barcode)
        {
            var pullCord = new GameObject("AI_PCFC");
            var comp = pullCord.GetComponent<PullCordForceChange>() ?? pullCord.AddComponent<PullCordForceChange>();
            comp.avatarCrate = new AvatarCrateReference(barcode);
            comp.rigManager = Player.RigManager;
            comp.ForceChange(Player.RigManager.gameObject);
            GameObject.Destroy(pullCord);
        }

        private static void SwapAvatar(string barcode, ModResult downloadResult = ModResult.SUCCEEDED)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode == Barcode.EMPTY)
                return;


            if (Player.RigManager == null)
                return;

            if (CrateFilterer.HasCrate<AvatarCrate>(new(barcode)))
            {
                ForceChange(barcode);
            }
            else
            {
                if (!ClientSettings.Downloading.DownloadAvatars.Value)
                    return;

                if (downloadResult == ModResult.FAILED)
                {
                    FusionModule.Logger.Error($"Download of avatar '{barcode}' has failed, not setting avatar");
                    return;
                }

                NetworkModRequester.RequestAndInstallMod(new NetworkModRequester.ModInstallInfo()
                {
                    Barcode = barcode,
                    Target = PlayerIDManager.LocalSmallID,
                    FinishDownloadCallback = (ev) => SwapAvatar(barcode, downloadResult: ev.result),
                    MaxBytes = DataConversions.ConvertMegabytesToBytes(ClientSettings.Downloading.MaxFileSize.Value),
                    HighPriority = true
                });
            }
        }
    }
}