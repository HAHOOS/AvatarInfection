using AvatarInfection.Managers;
using AvatarInfection.Utilities;

using LabFusion.Player;

using static AvatarInfection.Infection;

namespace AvatarInfection.Settings
{
    internal class InfectionSettings : SettingsCollection
    {
        #region Server

        internal AvatarSetting SelectedAvatar { get; set; }

        internal AvatarSetting ChildrenSelectedAvatar { get; set; }

        internal ServerSetting<bool> SyncWithInfected { get; set; }

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

        internal LocalSetting<InfectType> InfectType { get; set; }

        internal LocalSetting<int> TimeLimit { get; set; }

        internal LocalSetting<int> InfectedCount { get; set; }

        internal LocalSetting<int> HoldTime { get; set; }

        internal LocalSetting<bool> NoTimeLimit { get; set; }

        internal LocalSetting<bool> DontRepeatInfected { get; set; }

        internal LocalSetting<bool> TeleportOnStart { get; set; }

        internal LocalSetting<bool> SuicideInfects { get; set; }

        #endregion Local

        internal InfectionSettings()
        {
            DisableDevTools = CreateServerSetting(nameof(DisableDevTools), Constants.Defaults.DisableDeveloperTools);
            DisableSpawnGun = CreateServerSetting(nameof(DisableSpawnGun), Constants.Defaults.DisableSpawnGun);

            SelectedAvatar = CreateAvatarSetting(
                name: nameof(SelectedAvatar),
                value: Constants.Defaults.SelectMode,
                enabled: true,
                optional: false,
                groupName: "Infected Avatar",
                onValueChanged: SelectedPlayerOverride);

            ChildrenSelectedAvatar = CreateAvatarSetting(
                name: nameof(ChildrenSelectedAvatar),
                value: Constants.Defaults.SelectMode,
                enabled: false,
                optional: true,
                groupName: "Infected Children Avatar",
                onValueChanged: ChildrenSelectedPlayerOverride);
            ChildrenSelectedAvatar.DisallowedSelectModes = [AvatarSelectMode.FIRST_INFECTED];

            SyncWithInfected = CreateServerSetting(nameof(SyncWithInfected), Constants.Defaults.SyncWithInfected, onValueChanged: SyncWithInfectedUpdated);

            CountdownLength = CreateServerSetting(nameof(CountdownLength), Constants.Defaults.CountdownLength);

            AllowKeepInventory = CreateServerSetting(nameof(AllowKeepInventory), value: Constants.Defaults.AllowKeepInventory);

            TeleportOnEnd = CreateServerSetting(nameof(TeleportOnEnd), Constants.Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = CreateServerSetting(nameof(UseDeathmatchSpawns), Constants.Defaults.UseDeathMatchSpawns, onValueChanged: DeathmathUpdated);

            ShowCountdownToAll = CreateServerSetting(nameof(ShowCountdownToAll), Constants.Defaults.ShowCountdownToAll);

            FriendlyFire = CreateServerSetting(nameof(FriendlyFire), Constants.Defaults.FriendlyFire);

            DontRepeatInfected = CreateLocalSetting(nameof(DontRepeatInfected), Constants.Defaults.DontRepeatInfected);
            HoldTime = CreateLocalSetting(nameof(HoldTime), Constants.Defaults.HoldTime);
            InfectedCount = CreateLocalSetting(nameof(InfectedCount), Constants.Defaults.InfectedCount);
            InfectType = CreateLocalSetting(nameof(InfectType), Constants.Defaults._InfectType);
            NoTimeLimit = CreateLocalSetting(nameof(NoTimeLimit), Constants.Defaults.NoTimeLimit);
            SuicideInfects = CreateLocalSetting(nameof(SuicideInfects), Constants.Defaults.SuicideInfects);
            TeleportOnStart = CreateLocalSetting(nameof(TeleportOnStart), Constants.Defaults.TeleportOnStart);
            TimeLimit = CreateLocalSetting(nameof(TimeLimit), Constants.Defaults.TimeLimit);
        }

        private static void SyncWithInfectedUpdated()
        {
            if (!Instance.IsStarted)
                return;

            if (Instance.TeamManager.GetLocalTeam() == Instance.InfectedChildren)
                StatsManager.ApplyStats();
        }

        private void DeathmathUpdated()
        {
            if (!Instance.IsStarted)
                return;

            if (UseDeathmatchSpawns.Value)
                UseDeathmatchSpawns_Init(false);
            else
                ClearDeathmatchSpawns();
        }

        internal void SelectedPlayerOverride()
        {
            if (!Instance.IsStarted)
                return;

            if (Instance.TeamManager.GetLocalTeam() == Instance.Infected
                || (Instance.TeamManager.GetLocalTeam() == Instance.InfectedChildren && !ChildrenSelectedAvatar.Enabled))
            {
                Overrides.SetAvatarOverride(SelectedAvatar.Value.Barcode, SelectedAvatar.Value?.Origin ?? -1);
            }
        }

        internal void ChildrenSelectedPlayerOverride()
        {
            if (!Instance.IsStarted)
                return;

            if (Instance.TeamManager.GetLocalTeam() != Instance.InfectedChildren || !ChildrenSelectedAvatar.Enabled)
                return;

            Overrides.SetAvatarOverride(ChildrenSelectedAvatar.Value.Barcode, ChildrenSelectedAvatar.Value?.Origin ?? -1);
        }

        public void SetAvatar(string barcode, PlayerID player)
            => SelectedAvatar.SetAvatar(barcode, player);

        public void SetChildrenAvatar(string barcode, PlayerID player)
            => ChildrenSelectedAvatar.SetAvatar(barcode, player);
    }
}