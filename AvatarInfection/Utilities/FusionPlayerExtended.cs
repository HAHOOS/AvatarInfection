using System;
using System.Linq;

using BoneLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;
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

        public static float? AgilityOverride { get; private set; }

        public static float? StrengthUpperOverride { get; private set; }

        public static float? VitalityOverride { get; private set; }

        public static bool? MortalityOverride { get; private set; }

        public static string AvatarOverride { get; private set; }

        public static string LastAvatar { get; private set; }

        internal static void SetOverrides(float? speed, float? agility, float? strengthUpper, float? vitality, bool? mortality)
        {
            SpeedOverride = speed;
            AgilityOverride = agility;
            StrengthUpperOverride = strengthUpper;
            VitalityOverride = vitality;
            MortalityOverride = mortality;

            var rm = Player.RigManager;
            var avatar = rm?._avatar;

            if (avatar != null)
            {
                if (SetOverrideValue(AgilityOverride, avatar._agility, out bool changed1, out float res))
                    avatar._agility = res;

                if (SetOverrideValue(SpeedOverride, avatar._speed, out bool changed2, out float res2))
                    avatar._speed = res2;

                if (SetOverrideValue(StrengthUpperOverride, avatar._strengthUpper, out bool changed3, out float res3))
                {
                    avatar._strengthUpper = res3;
                    avatar._strengthGrip = res3;
                }

                if (SetOverrideValue(VitalityOverride, avatar._speed, out bool changed4, out float res4))
                    avatar._vitality = res4;

                SetMortality(mortality, out bool changed5);

                if (changed1 || changed2 || changed3 || changed4 || changed5)
                    rm.SwapAvatarCrate(rm.AvatarCrate.Barcode);
            }
        }

        private static bool SetOverrideValue(float? _override, float? actual, out bool changed, out float res)
        {
            changed = false;
            if (_override.HasValue && !actual.Equals(AgilityOverride.Value))
            {
                changed = true;
                res = _override.Value;
                return true;
            }
            res = -1f;
            return false;
        }

        private static void SetMortality(bool? mortality, out bool changed)
        {
            changed = false;
            var rm = Player.RigManager;
            if (rm?.health != null && mortality.HasValue)
            {
                Health.HealthMode mode = mortality.Value ? Health.HealthMode.Mortal : Health.HealthMode.Invincible;
                changed = rm.health.healthMode != mode;
                rm.health.SetHealthMode((int)mode);
            }
        }

        internal static void ClearAllOverrides()
        {
            // This is the worst way to do it, but I don't feel like overcomplicating this.
            // This only exists so that you don't change your avatar unnecessarily which under certain circumstances causes a lot of lags for a few seconds
            // Aka when 10 players change avatar to clear overrides
            if (AgilityOverride == null &&
                SpeedOverride == null &&
                StrengthUpperOverride == null &&
                VitalityOverride == null &&
                MortalityOverride == null)
            {
                return;
            }

            AgilityOverride = null;
            SpeedOverride = null;
            StrengthUpperOverride = null;
            VitalityOverride = null;
            MortalityOverride = null;
            if (Player.RigManager != null)
            {
                var rm = Player.RigManager;
                if (rm.AvatarCrate != null)
                    rm.SwapAvatarCrate(rm.AvatarCrate.Barcode);
            }
        }

        #region Avatar Override

        public static void SetAvatarOverride(string barcode, long origin = -1)
        {
            bool wasEmpty = string.IsNullOrEmpty(AvatarOverride);
            if (Player.RigManager != null && AssetWarehouse.ready && wasEmpty)
                LastAvatar = Player.RigManager.AvatarCrate.Barcode.ID ?? CommonBarcodes.Avatars.PolyBlank;

            AvatarOverride = barcode;
            SwapAvatar(barcode, origin);
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

        private static void SwapAvatar(string barcode, long origin = -1, ModResult downloadResult = ModResult.SUCCEEDED)
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

                if (origin > 0)
                    origin = (long)PlayerIDManager.GetHostID().PlatformID;

                var id = PlayerIDManager.PlayerIDs.FirstOrDefault(x => (long)x.PlatformID == origin);
                if (id == null)
                {
                    FusionModule.Logger.Error("Cannot download avatar '{barcode}', the player that has the avatar was not found");
                    return;
                }

                NetworkModRequester.RequestAndInstallMod(new NetworkModRequester.ModInstallInfo()
                {
                    Barcode = barcode,
                    Target = id.SmallID,
                    FinishDownloadCallback = (ev) => SwapAvatar(barcode, origin, ev.Result),
                    MaxBytes = DataConversions.ConvertMegabytesToBytes(ClientSettings.Downloading.MaxFileSize.Value),
                    HighPriority = true
                });
            }
        }

        #endregion Avatar Override
    }
}