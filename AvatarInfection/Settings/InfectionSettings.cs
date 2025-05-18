using static AvatarInfection.Infection;

namespace AvatarInfection.Settings
{
    internal class InfectionSettings : SettingsCollection
    {
        #region Server

        internal ServerSetting<bool> DisableSpawnGun { get; set; }

        internal ServerSetting<bool> DisableDevTools { get; set; }

        internal ServerSetting<string> SelectedAvatar { get; set; }

        internal ServerSetting<bool> AllowKeepInventory { get; set; }

        internal ServerSetting<int> CountdownLength { get; set; }

        internal ServerSetting<bool> TeleportOnEnd { get; set; }

        internal ServerSetting<bool> UseDeathmatchSpawns { get; set; }

        internal ServerSetting<bool> ShowCountdownToAll { get; set; }

        #endregion Server

        #region Local

        internal LocalSetting<AvatarSelectMode> SelectMode { get; set; }

        internal LocalSetting<bool> SyncWithInfected { get; set; }

        internal LocalSetting<int> TimeLimit { get; set; }

        internal LocalSetting<int> InfectedCount { get; set; }

        internal LocalSetting<bool> NoTimeLimit { get; set; }

        internal LocalSetting<bool> TeleportOnStart { get; set; }

        internal LocalSetting<InfectType> InfectType { get; set; }

        internal LocalSetting<bool> SuicideInfects { get; set; }

        internal LocalSetting<int> HoldTime { get; set; }

        #endregion Local

        internal InfectionSettings()
        {
            DisableDevTools = CreateServerSetting(nameof(DisableDevTools), Defaults.DisableDevTools);
            DisableSpawnGun = CreateServerSetting(nameof(DisableSpawnGun), Defaults.DisableSpawnGun);

            SelectedAvatar = CreateServerSetting(nameof(SelectedAvatar), string.Empty);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            CountdownLength = CreateServerSetting(nameof(CountdownLength), Defaults.CountdownLength);

            AllowKeepInventory = CreateServerSetting(nameof(AllowKeepInventory), value: Defaults.AllowKeepInventory);

            TeleportOnEnd = CreateServerSetting(nameof(TeleportOnEnd), Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = CreateServerSetting(nameof(UseDeathmatchSpawns), Defaults.UseDeathMatchSpawns);
            UseDeathmatchSpawns.OnValueChanged += () =>
            {
                if (UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(false);
                else
                    ClearDeathmatchSpawns();
            };

            ShowCountdownToAll = CreateServerSetting(nameof(ShowCountdownToAll), Defaults.ShowCountdownToAll);

            HoldTime = CreateLocalSetting(nameof(HoldTime), Defaults.HoldTime);
            InfectedCount = CreateLocalSetting(nameof(InfectedCount), Defaults.InfectedCount);
            InfectType = CreateLocalSetting(nameof(InfectType), Defaults._InfectType);
            NoTimeLimit = CreateLocalSetting(nameof(NoTimeLimit), Defaults.NoTimeLimit);
            SelectMode = CreateLocalSetting(nameof(SelectMode), Defaults.SelectMode);
            SuicideInfects = CreateLocalSetting(nameof(SuicideInfects), Defaults.SuicideInfects);
            SyncWithInfected = CreateLocalSetting(nameof(SyncWithInfected), Defaults.SyncWithInfected);
            TeleportOnStart = CreateLocalSetting(nameof(TeleportOnStart), Defaults.TeleportOnStart);
            TimeLimit = CreateLocalSetting(nameof(TimeLimit), Defaults.TimeLimit);
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