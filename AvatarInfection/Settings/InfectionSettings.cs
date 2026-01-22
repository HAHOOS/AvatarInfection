using AvatarInfection.Utilities;

using LabFusion.Player;

using static AvatarInfection.Infection;

namespace AvatarInfection.Settings
{
    internal class InfectionSettings : SettingsCollection
    {
        #region Server

        internal ServerSetting<string> SelectedAvatar { get; set; }

        /// <summary>
        /// The player that the selected avatar is from. Used when downloading missing avatar.
        /// </summary>
        internal ServerSetting<long> SelectedAvatar_Origin { get; set; }

        internal ServerSetting<bool> DisableSpawnGun { get; set; }

        internal ServerSetting<bool> DisableDevTools { get; set; }

        internal ServerSetting<bool> AllowKeepInventory { get; set; }

        internal ServerSetting<bool> TeleportOnEnd { get; set; }

        internal ServerSetting<bool> UseDeathmatchSpawns { get; set; }

        internal ServerSetting<bool> ShowCountdownToAll { get; set; }

        internal ServerSetting<bool> FriendlyFire { get; set; }

        internal ServerSetting<int> CountdownLength { get; set; }

        #endregion Server

        #region Local

        internal LocalSetting<AvatarSelectMode> SelectMode { get; set; }

        internal LocalSetting<bool> UseInfectedChildrenTeam { get; set; }

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

            SelectedAvatar_Origin = CreateServerSetting<long>(nameof(SelectedAvatar_Origin), -1);

            CountdownLength = CreateServerSetting(nameof(CountdownLength), Constants.Defaults.CountdownLength);

            AllowKeepInventory = CreateServerSetting(nameof(AllowKeepInventory), value: Constants.Defaults.AllowKeepInventory);

            TeleportOnEnd = CreateServerSetting(nameof(TeleportOnEnd), Constants.Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = CreateServerSetting(nameof(UseDeathmatchSpawns), Constants.Defaults.UseDeathMatchSpawns);
            UseDeathmatchSpawns.OnValueChanged += () =>
            {
                if (UseDeathmatchSpawns.Value)
                    UseDeathmatchSpawns_Init(false);
                else
                    ClearDeathmatchSpawns();
            };

            ShowCountdownToAll = CreateServerSetting(nameof(ShowCountdownToAll), Constants.Defaults.ShowCountdownToAll);

            FriendlyFire = CreateServerSetting(nameof(FriendlyFire), Constants.Defaults.FriendlyFire);
            HoldTime = CreateLocalSetting(nameof(HoldTime), Constants.Defaults.HoldTime);
            InfectedCount = CreateLocalSetting(nameof(InfectedCount), Constants.Defaults.InfectedCount);
            InfectType = CreateLocalSetting(nameof(InfectType), Constants.Defaults._InfectType);
            NoTimeLimit = CreateLocalSetting(nameof(NoTimeLimit), Constants.Defaults.NoTimeLimit);
            SelectMode = CreateLocalSetting(nameof(SelectMode), Constants.Defaults.SelectMode);
            SuicideInfects = CreateLocalSetting(nameof(SuicideInfects), Constants.Defaults.SuicideInfects);
            UseInfectedChildrenTeam = CreateLocalSetting(nameof(UseInfectedChildrenTeam), Constants.Defaults.AddInfectedChildrenTeam);
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

            Overrides.SetAvatarOverride(SelectedAvatar.Value);
        }

        public void SetAvatar(string barcode, PlayerID player)
        {
            SelectedAvatar_Origin.Value = (long)player.PlatformID;
            SelectedAvatar.Value = barcode;
        }
    }
}