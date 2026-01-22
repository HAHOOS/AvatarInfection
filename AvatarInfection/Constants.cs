using AvatarInfection.Settings;

using Il2CppSLZ.Marrow.Warehouse;

using static AvatarInfection.Infection;

namespace AvatarInfection
{
    internal static class Constants
    {
        public const string Name = "AvatarInfection";

        public const string Description = "An infection is spreading, turning people into a selected avatar by the host.";

        public const string Author = "HAHOOS";

        public const string Version = "2.0.0";

        public const string Barcode = $"{Author}.{Name}";

        public const int InfectedBitReward = 50;

        public const int SurvivorsBitReward = 75;

        public static readonly MonoDiscReference[] Tracks =
        [
            Disk("TheRecurringDream"),
            Disk("HeavySteps"),
            Disk("StankFace"),
            Disk("AlexinWonderland"),
            Disk("ItDoBeGroovin"),
            Disk("ConcreteCrypt"),
        ];

        internal static MonoDiscReference Disk(string name) => new($"SLZ.BONELAB.Content.MonoDisc.{name}");

        internal static class Defaults
        {
            public const int TimeLimit = 10;

            public const bool NoTimeLimit = false;

            public readonly static TeamSettings InfectedStats = new()
            {
                Vitality = new(0.75f),
                JumpPower = new(1.5f),
                Speed = new(2.8f),
                Agility = new(2f),
                StrengthUpper = new(0.5f),

                Mortality = true,

                CanUseGuns = false,
            };

            public readonly static TeamSettings InfectedChildrenStats = new()
            {
                Vitality = new(0.5f),
                JumpPower = new(1.25f),
                Speed = new(1.8f),
                Agility = new(1.5f),
                StrengthUpper = new(0.35f),

                Mortality = true,

                CanUseGuns = false,
            };

            public readonly static TeamSettings SurvivorsStats = new()
            {
                Vitality = new(1f),
                JumpPower = new(1f),
                Speed = new(1.2f),
                Agility = new(1f),
                StrengthUpper = new(1.5f),

                Mortality = true,

                CanUseGuns = true,
            };

            public const bool DisableSpawnGun = true;

            public const bool DisableDeveloperTools = true;

            public const bool AllowKeepInventory = false;

            public const int InfectedCount = 1;

            public const bool TeleportOnStart = true;

            public const int CountdownLength = 30;

            public const InfectType _InfectType = InfectType.TOUCH;

            public const bool SuicideInfects = true;

            public const int HoldTime = 0;

            public const bool TeleportOnEnd = false;

            public const bool UseDeathMatchSpawns = true;

            public const bool SyncWithInfected = false;

            public const bool ShowCountdownToAll = false;

            public const bool FriendlyFire = true;

            public const bool DontRepeatInfected = true;

            public const AvatarSelectMode SelectMode = AvatarSelectMode.CONFIG;
        }
    }
}