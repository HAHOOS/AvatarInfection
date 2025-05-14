using LabFusion.SDK.Metadata;

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
        {
            LabPresence.Gamemodes.RegisterGamemode(Infection.Defaults.Barcode,
                () => $"{Infection.Instance.Survivors.PlayerCount} survivors left!", () =>
            {
                if ((Infection.Instance.EndUnix as MetadataVariable).GetValue() != null
                    && (Infection.Instance.StartUnix as MetadataVariable).GetValue() != null)
                {
                    var value = Infection.Instance.EndUnix.GetValue();
                    return new((ulong?)Infection.Instance.StartUnix.GetValue(), value == -1 ? null : (ulong?)value);
                }
                return null;
            });
        }
    }
}