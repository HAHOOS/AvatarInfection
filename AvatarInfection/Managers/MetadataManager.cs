using System.Linq;

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

        // TODO: fix this shit not working
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

        public static void SetAllMetadata()
        {
            IHaveAvatarInfection();
            SetAvatarModId();
        }

        public static void SetAvatarModId()
        {
            if (AssetWarehouse.Instance?.initialized != true)
                return;

            if (Player.RigManager == null)
                return;

            var pallet = Player.RigManager.AvatarCrate.Crate?.Pallet;
            if (pallet == null)
                return;

            var modId = CrateFilterer.GetModID(pallet);

            LocalPlayer.Metadata.Metadata.TrySetMetadata(AVATAR_MOD_ID, modId.ToString());
        }

        public static bool IsAvatarDownloadable(PlayerID player)
            => player.Metadata.Metadata.TryGetMetadata(AVATAR_MOD_ID, out string val)
            && !string.IsNullOrWhiteSpace(val) && int.TryParse(val, out int res) && res != -1;
    }
}