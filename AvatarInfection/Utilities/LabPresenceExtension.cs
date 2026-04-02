using LabPresence.Managers;

using MelonLoader;

namespace AvatarInfection.Utilities
{
    internal static class LabPresenceExtension
    {
        public const string SmallImage = "hahoos_avatarinfection";

        public static MelonBase LabPresenceMelon => Core.FindMelon("LabPresence", "HAHOOS");

        public static void Init()
        {
            if (LabPresenceMelon != null && LabPresenceMelon.Info?.SemanticVersion >= new Semver.SemVersion(1, 3, 0))
                Internal_Init();
            else
                FusionModule.Logger.Warn("Could not find LabPresence (minimum version required: v1.3.0)");
        }

        internal static void Internal_Init()
            => GamemodeManager.RegisterGamemode(Constants.Barcode, CustomToolTip, CustomTimestamp, SmallImage);

        private static string CustomToolTip()
            => $"{Infection.Instance.Survivors.PlayerCount} survivors left!";

        private static Timestamp CustomTimestamp()
        {
            if (Infection.Instance.Config.StartUnix.Value == -1 && Infection.Instance.Config.EndUnix.Value == -1)
                return null;

            long start = Infection.Instance.Config.StartUnix.Value;
            long? end = Infection.Instance.Config.EndUnix.Value;
            if (end == -1)
                end = null;

            return new((ulong?)start, (ulong?)end);
        }
    }
}