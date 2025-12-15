using AvatarInfection.Utilities;

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

        internal LocalSetting<bool> AddInfectedChildrenTeam { get; set; }

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
            DisableDevTools = CreateServerSetting(nameof(DisableDevTools), Constants.Defaults.DisableDeveloperTools);
            DisableSpawnGun = CreateServerSetting(nameof(DisableSpawnGun), Constants.Defaults.DisableSpawnGun);

            SelectedAvatar = CreateServerSetting(nameof(SelectedAvatar), string.Empty);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            CountdownLength = CreateServerSetting(nameof(CountdownLength), Constants.Defaults.CountdownLength);

            AllowKeepInventory = CreateServerSetting(nameof(AllowKeepInventory), value: Constants.Defaults.AllowKeepInventory);

            TeleportOnEnd = CreateServerSetting(nameof(TeleportOnEnd), Constants.Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = CreateServerSetting(nameof(UseDeathmatchSpawns), Constants.Defaults.UseDeathMatchSpawns);
            UseDeathmatchSpawns.OnValueChanged += () =>
            {
                if (UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(false);
                else
                    ClearDeathmatchSpawns();
            };

            ShowCountdownToAll = CreateServerSetting(nameof(ShowCountdownToAll), Constants.Defaults.ShowCountdownToAll);

            HoldTime = CreateLocalSetting(nameof(HoldTime), Constants.Defaults.HoldTime);
            InfectedCount = CreateLocalSetting(nameof(InfectedCount), Constants.Defaults.InfectedCount);
            InfectType = CreateLocalSetting(nameof(InfectType), Constants.Defaults._InfectType);
            NoTimeLimit = CreateLocalSetting(nameof(NoTimeLimit), Constants.Defaults.NoTimeLimit);
            SelectMode = CreateLocalSetting(nameof(SelectMode), Constants.Defaults.SelectMode);
            SuicideInfects = CreateLocalSetting(nameof(SuicideInfects), Constants.Defaults.SuicideInfects);
            AddInfectedChildrenTeam = CreateLocalSetting(nameof(AddInfectedChildrenTeam), Constants.Defaults.AddInfectedChildrenTeam);
            TeleportOnStart = CreateLocalSetting(nameof(TeleportOnStart), Constants.Defaults.TeleportOnStart);
            TimeLimit = CreateLocalSetting(nameof(TimeLimit), Constants.Defaults.TimeLimit);
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

            FusionPlayerExtended.SetAvatarOverride(SelectedAvatar.ClientValue);
        }
    }
}