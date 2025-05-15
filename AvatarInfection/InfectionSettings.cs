using LabFusion.Utilities;

using MelonLoader;

using static AvatarInfection.Infection;

namespace AvatarInfection
{
    internal class InfectionSettings
    {
        internal ServerSetting<bool> DisableSpawnGun { get; set; }

        internal ServerSetting<bool> DisableDevTools { get; set; }

        internal ServerSetting<string> SelectedAvatar { get; set; }

        internal int TimeLimit { get; set; } = Defaults.TimeLimit;

        internal MelonPreferences_Entry<int> TimeLimit_Entry { get; set; }

        internal int InfectedCount { get; set; } = Defaults.InfectedCount;

        internal MelonPreferences_Entry<int> InfectedCount_Entry { get; set; }

        internal bool NoTimeLimit { get; set; } = Defaults.UntilAllFound;

        internal MelonPreferences_Entry<bool> NoTimeLimit_Entry { get; set; }

        internal bool TeleportOnStart { get; set; } = Defaults.ShouldTeleportToHost;

        internal MelonPreferences_Entry<bool> TeleportOnStart_Entry { get; set; }

        internal InfectType InfectType { get; set; } = Defaults._InfectType;

        internal MelonPreferences_Entry<InfectType> InfectType_Entry { get; set; }

        internal ServerSetting<bool> AllowKeepInventory { get; set; }

        internal ServerSetting<int> CountdownLength { get; set; }

        internal ServerSetting<bool> DontShowAnyNametags { get; set; }

        internal bool SuicideInfects { get; set; } = Defaults.SuicideInfects;

        internal MelonPreferences_Entry<bool> SuicideInfects_Entry { get; set; }

        internal int HoldTime { get; set; } = Defaults.HoldTime;

        internal MelonPreferences_Entry<int> HoldTime_Entry { get; set; }

        internal ServerSetting<bool> TeleportOnEnd { get; set; }

        internal ServerSetting<bool> UseDeathmatchSpawns { get; set; }

        internal bool SyncWithInfected { get; set; } = Defaults.SyncWithInfected;

        internal MelonPreferences_Entry<bool> SyncWithInfected_Entry { get; set; }

        internal AvatarSelectMode SelectMode { get; set; } = Defaults.SelectMode;

        internal MelonPreferences_Entry<AvatarSelectMode> SelectMode_Entry { get; set; }

        internal ServerSetting<bool> ShowCountdownToAll { get; set; }

        internal InfectionSettings()
        {
            DisableDevTools = new(Instance, nameof(DisableDevTools), Defaults.DisableDevTools);
            DisableSpawnGun = new(Instance, nameof(DisableSpawnGun), Defaults.DisableSpawnGun);

            SelectedAvatar = new(Instance, nameof(SelectedAvatar), null);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            CountdownLength = new(Instance, nameof(CountdownLength), Defaults.CountdownLength);

            AllowKeepInventory = new(Instance, nameof(AllowKeepInventory), value: Defaults.AllowKeepInventory);

            TeleportOnEnd = new(Instance, nameof(TeleportOnEnd), Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = new(Instance, nameof(UseDeathmatchSpawns), Defaults.UseDeathMatchSpawns);
            UseDeathmatchSpawns.OnValueChanged += () =>
            {
                if (UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(false);
                else
                    ClearDeathmatchSpawns();
            };

            ShowCountdownToAll = new(Instance, nameof(ShowCountdownToAll), Defaults.ShowCountdownToAll);
            DontShowAnyNametags = new(Instance, nameof(DontShowAnyNametags), Defaults.DontShowAnyNametags);
            DontShowAnyNametags.OnValueChanged += FusionOverrides.ForceUpdateOverrides;

            HoldTime_Entry = Core.Category.CreateEntry(nameof(HoldTime), HoldTime);
            InfectedCount_Entry = Core.Category.CreateEntry(nameof(InfectedCount), InfectedCount);
            InfectType_Entry = Core.Category.CreateEntry(nameof(InfectType), InfectType);
            NoTimeLimit_Entry = Core.Category.CreateEntry(nameof(NoTimeLimit), NoTimeLimit);
            SelectMode_Entry = Core.Category.CreateEntry(nameof(SelectMode), SelectMode);
            SuicideInfects_Entry = Core.Category.CreateEntry(nameof(SuicideInfects), SuicideInfects);
            SyncWithInfected_Entry = Core.Category.CreateEntry(nameof(SyncWithInfected), SyncWithInfected);
            TeleportOnStart_Entry = Core.Category.CreateEntry(nameof(TeleportOnStart), TeleportOnStart);
            TimeLimit_Entry = Core.Category.CreateEntry(nameof(TimeLimit), TimeLimit);
        }

        internal void Save()
        {
            DisableDevTools.Save();
            DisableSpawnGun.Save();
            SelectedAvatar.Save();

            AllowKeepInventory.Save();
            CountdownLength.Save();
            DontShowAnyNametags.Save();

            TeleportOnEnd.Save();
            UseDeathmatchSpawns.Save();
            ShowCountdownToAll.Save();

            HoldTime_Entry.Value = HoldTime;
            InfectedCount_Entry.Value = InfectedCount;
            InfectType_Entry.Value = InfectType;
            NoTimeLimit_Entry.Value = NoTimeLimit;
            SelectMode_Entry.Value = SelectMode;
            SuicideInfects_Entry.Value = SuicideInfects;
            SyncWithInfected_Entry.Value = SyncWithInfected;
            TeleportOnStart_Entry.Value = TeleportOnStart;
            TimeLimit_Entry.Value = TimeLimit;

            Infection.Instance.InfectedMetadata.Save();
            Infection.Instance.SurvivorsMetadata.Save();
            Infection.Instance.InfectedChildrenMetadata.Save();

            Core.Category.SaveToFile();
        }

        internal void Load()
        {
            DisableDevTools.Load();
            DisableSpawnGun.Load();
            SelectedAvatar.Load();

            AllowKeepInventory.Load();
            CountdownLength.Load();
            DontShowAnyNametags.Load();

            TeleportOnEnd.Load();
            UseDeathmatchSpawns.Load();
            ShowCountdownToAll.Load();

            HoldTime = HoldTime_Entry.Value;
            InfectedCount = InfectedCount_Entry.Value;
            InfectType = InfectType_Entry.Value;
            NoTimeLimit = NoTimeLimit_Entry.Value;
            SelectMode = SelectMode_Entry.Value;
            SuicideInfects = SuicideInfects_Entry.Value;
            SyncWithInfected = SyncWithInfected_Entry.Value;
            TeleportOnStart = TeleportOnStart_Entry.Value;
            TimeLimit = TimeLimit_Entry.Value;

            Infection.Instance.InfectedMetadata.Load();
            Infection.Instance.SurvivorsMetadata.Load();
            Infection.Instance.InfectedChildrenMetadata.Load();
        }

        internal void SelectedPlayerOverride()
        {
            if (!Instance.IsStarted)
                return;

            if (Instance.TeamManager.GetLocalTeam() != Instance.Infected
                && Instance.TeamManager.GetLocalTeam() != Instance.InfectedChildren)
            {
                return;
            }

            SwapAvatar(SelectedAvatar.ClientValue);
        }
    }
}