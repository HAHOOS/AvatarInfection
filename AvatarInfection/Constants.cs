using AvatarInfection.Settings;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Bonelab;

using static AvatarInfection.Infection;

namespace AvatarInfection
{
    internal static class Constants
    {
        public const string Version = "1.1.0";

        internal static class Defaults
        {
            public const string Barcode = "HAHOOS.AvatarInfection";

            public const int TimeLimit = 10;

            public const int InfectedBitReward = 50;

            public const int SurvivorsBitReward = 75;

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

            public const AvatarSelectMode SelectMode = AvatarSelectMode.CONFIG;

            // Have no idea for mono discs
            // TODO: Add more "original" tracks and not just have the same as Hide & Seek
            public static readonly MonoDiscReference[] Tracks =
        [
            BonelabMonoDiscReferences.TheRecurringDreamReference,
            BonelabMonoDiscReferences.HeavyStepsReference,
            BonelabMonoDiscReferences.StankFaceReference,
            BonelabMonoDiscReferences.AlexInWonderlandReference,
            BonelabMonoDiscReferences.ItDoBeGroovinReference,

            BonelabMonoDiscReferences.ConcreteCryptReference, // concrete crypt
        ];
        }
    }
}