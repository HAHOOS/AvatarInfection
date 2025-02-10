using AvatarInfection.Utilities;

using LabFusion.SDK.Metadata;

namespace AvatarInfection.Helper
{
    public static class MetadataHelper
    {
        public static bool IsEmpty(this MetadataVariable metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.Key));
        }

        public static bool IsEmpty<T>(this MetadataVariableT<T> metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.Key));
        }

        public static bool IsEmpty(this ToggleMetadataVariable metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.Key));
        }

        public static bool IsEmpty<T>(this ToggleMetadataVariableT<T> metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.Key));
        }

        public static bool IsToggledEmpty(this ToggleMetadataVariable metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.ToggledKey));
        }

        public static bool IsToggledEmpty<T>(this ToggleMetadataVariableT<T> metadata)
        {
            return string.IsNullOrWhiteSpace(metadata.Metadata.GetMetadata(metadata.ToggledKey));
        }
    }
}