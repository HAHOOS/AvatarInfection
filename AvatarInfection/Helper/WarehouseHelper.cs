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
        /// Excludes all avatars that are not downloadable (and aren't in base game pallets)
        /// </summary>
        public static void ExcludeNonPublic(this List<AvatarCrate> list)
            => list.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => !x.IsPublic()));

        /// <summary>
        /// Can the crate be downloaded or is the crate part of the base game pallets
        /// </summary>
        public static bool IsPublic(this Crate crate)
            => !(CrateFilterer.GetModID(crate.Pallet) == -1 && !AssetWarehouse.Instance.gamePallets.Contains(crate.Pallet.Barcode));
    }
}