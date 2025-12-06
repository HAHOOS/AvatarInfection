using System.Threading.Channels;

using BoneLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Bonelab.SaveData;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;

using LabFusion.Data;
using LabFusion.Downloading;
using LabFusion.Marrow;
using LabFusion.Player;
using LabFusion.Preferences.Client;
using LabFusion.RPC;
using LabFusion.Utilities;

using UnityEngine;

using static BoneLib.CommonBarcodes;

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
            LocalAvatar.AvatarOverride = barcode;
        }

        public static void ClearAvatarOverride()
        {
            AvatarOverride = null;
            LocalAvatar.AvatarOverride = null;
            if (Player.RigManager != null && LastAvatar != null && AssetWarehouse.ready)
            {
                var rm = Player.RigManager;
                var last = LastAvatar;
                LastAvatar = null;
                rm.SwapAvatarCrate(new Barcode(last), true);
                DataManager.ActiveSave.PlayerSettings.CurrentAvatar = last;
            }
        }

        internal static void SwapAvatar(string barcode, ModResult downloadResult = ModResult.SUCCEEDED)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode == Barcode.EMPTY)
            {
                FusionModule.Logger.Error("ALERT! ALERT! This is not supposed to fucking happen, what the fuck did you do that the SelectedAvatar is empty. Now relax, calm down and fix this issue\nfuck you rottencheese, this shit will never work.");
                return;
            }

            if (Player.RigManager == null)
                return;

            if (CrateFilterer.HasCrate<AvatarCrate>(new(barcode)))
            {
                var obj = new GameObject("AI_PCFC");
                var comp = obj.AddComponent<PullCordForceChange>();
                comp.avatarCrate = new AvatarCrateReference(barcode);
                comp.rigManager = Player.RigManager;
                comp.ForceChange(comp.rigManager.gameObject);
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
                    FinishDownloadCallback = (ev) => SwapAvatar(barcode, ev.result),
                    MaxBytes = DataConversions.ConvertMegabytesToBytes(ClientSettings.Downloading.MaxFileSize.Value),
                    HighPriority = true
                });
            }
        }
    }
}