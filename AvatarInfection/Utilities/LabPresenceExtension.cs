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
            LabPresence.Gamemodes.RegisterGamemode(Infection.Defaults.Barcode, () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != Infection.Defaults.Barcode)
                    return string.Empty;

                var gamemode = (Infection)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                return $"{Infection.Survivors.PlayerCount} survivors left!";
            }, () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != Infection.Defaults.Barcode)
                    return null;

                var gamemode = (Infection)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                if ((gamemode.EndUnix as MetadataVariable).GetValue() != null && (gamemode.StartUnix as MetadataVariable).GetValue() != null)
                {
                    var value = gamemode.EndUnix.GetValue();
                    return new((ulong?)gamemode.StartUnix.GetValue(), value == -1 ? null : (ulong?)value);
                }
                return null;
            });
        }
    }
}