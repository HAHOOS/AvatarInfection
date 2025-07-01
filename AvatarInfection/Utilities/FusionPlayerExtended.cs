using BoneLib;

using Il2CppSLZ.Bonelab.SaveData;
using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Marrow;
using LabFusion.Player;
using LabFusion.Utilities;

namespace AvatarInfection.Utilities
{
    public static class FusionPlayerExtended
    {
        public static float? SpeedOverride { get; private set; } = null;

        public static float? JumpPowerOverride { get; private set; } = null;

        public static float? AgilityOverride { get; private set; } = null;

        public static float? StrengthUpperOverride { get; private set; } = null;

        // Avatar override calls everything in FusionPlayer but restores the old avatar
        public static string AvatarOverride { get; private set; } = null;

        public static string LastAvatar { get; private set; } = null;

        internal static void SetOverrides(float? jumpPower, float? speed, float? agility, float? strengthUpper)
        {
            SpeedOverride = speed;
            JumpPowerOverride = jumpPower;
            AgilityOverride = agility;
            StrengthUpperOverride = strengthUpper;
            if (Player.RigManager != null)
            {
                var rm = Player.RigManager;
                var avatar = rm._avatar;
                if (avatar != null)
                {
                    bool changed = false;
                    if (AgilityOverride.HasValue)
                    {
                        if (avatar._agility != AgilityOverride.Value)
                        {
                            changed = true;
                            avatar._agility = AgilityOverride.Value;
                        }
                    }
                    if (JumpPowerOverride.HasValue)
                    {
                        if (avatar._strengthLower != JumpPowerOverride.Value)
                        {
                            changed = true;
                            avatar._strengthLower = JumpPowerOverride.Value;
                        }
                    }
                    if (SpeedOverride.HasValue)
                    {
                        if (avatar._speed != SpeedOverride.Value)
                        {
                            changed = true;
                            avatar._speed = SpeedOverride.Value;
                        }
                    }
                    if (StrengthUpperOverride.HasValue)
                    {
                        if (avatar._strengthUpper != StrengthUpperOverride.Value)
                        {
                            changed = true;
                            avatar._strengthUpper = StrengthUpperOverride.Value;
                            avatar._strengthGrip = StrengthUpperOverride.Value;
                        }
                    }
                    if (changed)
                        rm.SwapAvatarCrate(rm.AvatarCrate.Barcode);
                }
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
            AvatarOverride = barcode;
            LocalAvatar.AvatarOverride = barcode;
            if (Player.RigManager != null && AssetWarehouse.ready && AvatarOverride != null)
            {
                if (wasEmpty)
                    LastAvatar = Player.RigManager.AvatarCrate.Barcode.ID ?? CommonBarcodes.Avatars.PolyBlank;
            }
        }

        public static void ClearAvatarOverride()
        {
            AvatarOverride = null;
            LocalAvatar.AvatarOverride = null;
            if (Player.RigManager != null && LastAvatar != null && AssetWarehouse.ready)
            {
                var rm = Player.RigManager;
                rm.SwapAvatarCrate(new Barcode(LastAvatar), true);
                DataManager.ActiveSave.PlayerSettings.CurrentAvatar = LastAvatar;
                LastAvatar = null;
            }
        }
    }
}