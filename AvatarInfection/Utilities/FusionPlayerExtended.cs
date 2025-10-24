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
            if (Player.RigManager != null)
            {
                var rm = Player.RigManager;
                var avatar = rm._avatar;
                if (avatar != null)
                {
                    bool changed = false;
                    if (AgilityOverride.HasValue && !avatar._agility.Equals(AgilityOverride.Value))
                    {
                        changed = true;
                        avatar._agility = AgilityOverride.Value;
                    }
                    if (JumpPowerOverride.HasValue && !avatar._strengthLower.Equals(JumpPowerOverride.Value))
                    {
                        changed = true;
                        avatar._strengthLower = JumpPowerOverride.Value;
                    }
                    if (SpeedOverride.HasValue && !avatar._speed.Equals(SpeedOverride.Value))
                    {
                        changed = true;
                        avatar._speed = SpeedOverride.Value;
                    }
                    if (StrengthUpperOverride.HasValue && !avatar._strengthUpper.Equals(StrengthUpperOverride.Value))
                    {
                        changed = true;
                        avatar._strengthUpper = StrengthUpperOverride.Value;
                        avatar._strengthGrip = StrengthUpperOverride.Value;
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
    }
}