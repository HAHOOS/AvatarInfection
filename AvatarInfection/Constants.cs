using AvatarInfection.Settings;

using Il2CppSLZ.Marrow.Warehouse;

using static AvatarInfection.Infection;

namespace AvatarInfection
{
    internal static class Constants
    {
        public const string Name = "<color=#73FF00>A</color><color=#6BFA02>v</color><color=#64F604>a</color><color=#5DF106>t</color><color=#56ED09>a</color><color=#4FE80B>r</color> <color=#40DF10>I</color><color=#39DB12>n</color><color=#32D714>f</color><color=#2BD217>e</color><color=#23CE19>c</color><color=#1CC91B>t</color><color=#15C51E>i</color><color=#0EC020>o</color><color=#07BC22>n</color>";

        public const string PlainName = "Avatar Infection";

        // This probably needs to ACTUALLY explain how the gamemode works, but with all the settings its hard to explain everything.
        public const string Description = "An infection is spreading, turning people into a selected avatar by the host.";

        public const string Author = "HAHOOS";

        public const string Version = "2.0.0";

        public const string Barcode = $"{Author}.{PlainName}";

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
                Height = new(1f, false),
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
                Height = new(1f, false),
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
                Height = new(1f, false),
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