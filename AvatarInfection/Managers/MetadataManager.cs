using BoneLib;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Extensions;
using LabFusion.Marrow;
using LabFusion.Player;

namespace AvatarInfection.Managers
{
    public static class MetadataManager
    {
        public const string HAS_AVATAR_INFECTION_KEY = "DoYouHaveAvatarInfection";

        public const string AVATAR_MOD_ID = "AvatarInfection-AvatarModId";

        // Could the name be better? Yes
        // Will I improve it? No
        public static void IHaveAvatarInfection()
            => LocalPlayer.Metadata.Metadata.TrySetMetadata(HAS_AVATAR_INFECTION_KEY, bool.TrueString);

        public static bool DoYouHaveAvatarInfection(PlayerID player)
        => player.Metadata.Metadata.TryGetMetadata(HAS_AVATAR_INFECTION_KEY, out string val)
            && !string.IsNullOrWhiteSpace(val) && bool.TryParse(val, out bool res) && res;

        public static int CountPlayersWithAvatarInfection()
        {
            int plrs = 0;
            PlayerIDManager.PlayerIDs.ForEach(x =>
            {
                if (!x.Metadata.Metadata.TryGetMetadata(HAS_AVATAR_INFECTION_KEY, out string val))
                    return;
                if (string.IsNullOrWhiteSpace(val))
                    return;

                if (val == bool.TrueString)
                    plrs++;
            });
            return plrs;
        }

        public static bool IsAvatarDownloadable(this PlayerID player)
            => !string.IsNullOrWhiteSpace(player?.Metadata?.AvatarModID?.GetValueOrEmpty())
            && player.Metadata.AvatarModID.GetValue() != -1;
    }
}