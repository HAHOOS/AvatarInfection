using System;

using AvatarInfection.Utilities;

using LabFusion.Player;

using static AvatarInfection.Infection;

namespace AvatarInfection.Settings
{
    internal class InfectionSettings : SettingsCollection
    {
        #region Server

        internal ServerSetting<SelectedAvatarData> SelectedAvatar { get; set; }

        // TODO: simplify creating multiple SelectedAvatar settings
        internal ToggleServerSetting<SelectedAvatarData> ChildrenSelectedAvatar { get; set; }

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

        internal LocalSetting<AvatarSelectMode> SelectMode { get; set; }

        internal LocalSetting<ChildrenAvatarSelectMode> ChildrenSelectMode { get; set; }

        internal LocalSetting<int> TimeLimit { get; set; }

        internal LocalSetting<int> InfectedCount { get; set; }

        internal LocalSetting<bool> NoTimeLimit { get; set; }

        internal LocalSetting<bool> DontRepeatInfected { get; set; }

        internal LocalSetting<bool> TeleportOnStart { get; set; }

        internal LocalSetting<InfectType> InfectType { get; set; }

        internal LocalSetting<bool> SuicideInfects { get; set; }

        internal LocalSetting<int> HoldTime { get; set; }

        #endregion Local

        internal InfectionSettings()
        {
            DisableDevTools = CreateServerSetting(nameof(DisableDevTools), Constants.Defaults.DisableDeveloperTools);
            DisableSpawnGun = CreateServerSetting(nameof(DisableSpawnGun), Constants.Defaults.DisableSpawnGun);

            SelectedAvatar = CreateServerSetting<SelectedAvatarData>(nameof(SelectedAvatar), null);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            ChildrenSelectedAvatar = CreateToggleServerSetting<SelectedAvatarData>(nameof(ChildrenSelectedAvatar), null, false);
            ChildrenSelectedAvatar.OnValueChanged += ChildrenSelectedPlayerOverride;

            SyncWithInfected = CreateServerSetting(nameof(SyncWithInfected), Constants.Defaults.SyncWithInfected);

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

            DontRepeatInfected = CreateLocalSetting(nameof(DontRepeatInfected), Constants.Defaults.DontRepeatInfected);
            HoldTime = CreateLocalSetting(nameof(HoldTime), Constants.Defaults.HoldTime);
            InfectedCount = CreateLocalSetting(nameof(InfectedCount), Constants.Defaults.InfectedCount);
            InfectType = CreateLocalSetting(nameof(InfectType), Constants.Defaults._InfectType);
            NoTimeLimit = CreateLocalSetting(nameof(NoTimeLimit), Constants.Defaults.NoTimeLimit);
            SelectMode = CreateLocalSetting(nameof(SelectMode), Constants.Defaults.SelectMode);
            ChildrenSelectMode = CreateLocalSetting(nameof(ChildrenSelectMode), (ChildrenAvatarSelectMode)Constants.Defaults.SelectMode);
            SuicideInfects = CreateLocalSetting(nameof(SuicideInfects), Constants.Defaults.SuicideInfects);
            TeleportOnStart = CreateLocalSetting(nameof(TeleportOnStart), Constants.Defaults.TeleportOnStart);
            TimeLimit = CreateLocalSetting(nameof(TimeLimit), Constants.Defaults.TimeLimit);
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
            => SelectedAvatar.Value = new(barcode, (long)player.PlatformID);

        public void SetChildrenAvatar(string barcode, PlayerID player)
            => ChildrenSelectedAvatar.Value = new(barcode, (long)player.PlatformID);
    }

    internal class SelectedAvatarData(string barcode, long origin = -1) : IEquatable<SelectedAvatarData>
    {
        public string Barcode { get; set; } = barcode;

        public long Origin { get; set; } = origin;

        public override bool Equals(object obj)
            => obj is SelectedAvatarData data && data.Barcode == Barcode && data.Origin == Origin;

        public bool Equals(SelectedAvatarData other)
            => other is not null && other.Barcode == Barcode && other.Origin == Origin;

        public override int GetHashCode()
            => Barcode.GetHashCode() + Origin.GetHashCode();
    }
}