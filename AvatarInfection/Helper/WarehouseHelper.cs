using Il2CppSLZ.Marrow.Warehouse;

using Il2CppSystem.Collections.Generic;

using LabFusion.Marrow;

namespace AvatarInfection.Helper
{
    public static class WarehouseHelper
    {
        public static void ExcludeRedacted(this List<AvatarCrate> list)
            => list.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));

        /// <summary>
        /// Excludes all mods that are not downloadable (and aren't in base game pallets)
        /// </summary>
        public static void ExcludeNonPublic(this List<AvatarCrate> list)
            => list.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => !x.IsPublicAvatar()));

        /// <summary>
        /// Can the avatar be downloaded or is the avatar part of the base game pallets
        /// </summary>
        public static bool IsPublicAvatar(this AvatarCrate crate)
            => !(CrateFilterer.GetModID(crate.Pallet) == -1 && !AssetWarehouse.Instance.gamePallets.Contains(crate.Pallet.Barcode));
    }
}