using LabFusion.SDK.Metadata;

using LabPresence;

using MelonLoader;

namespace AvatarInfection.Utilities
{
    internal static class LabPresenceExtension
    {
        public static MelonBase LabPresenceMelon => Core.FindMelon("LabPresence", "HAHOOS");

        public static void Init()
        {
            if (LabPresenceMelon != null && LabPresenceMelon.Info?.SemanticVersion > new Semver.SemVersion(1, 0, 0))
                Internal_Init();
            else
                FusionModule.Logger.Warn("Could not find LabPresence");
        }

        internal static void Internal_Init()
            => Gamemodes.RegisterGamemode(Constants.Barcode, CustomToolTip, CustomTimestamp);

        private static string CustomToolTip()
            => $"{Infection.Instance.Survivors.PlayerCount} survivors left!";

        private static Timestamp CustomTimestamp()
        {
            if (NotNull(Infection.Instance.EndUnix) && NotNull(Infection.Instance.StartUnix))
            {
                var start = (ulong?)Infection.Instance.StartUnix.GetValue();
                var end = Infection.Instance.EndUnix.GetValue();
                if (end == -1)
                    end = null;

                return new(start, (ulong?)end);
            }
            return null;
        }

        private static bool NotNull(MetadataVariable val)
            => val.GetValue() != null;
    }
}