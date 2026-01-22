using System;

using AvatarInfection.Managers;

using BoneLib;

using Il2CppSLZ.Marrow;

using LabFusion.Extensions;
using LabFusion.Network;
using LabFusion.SDK.Gamemodes;

namespace AvatarInfection.Settings
{
    public class TeamMetadata : SettingsCollection
    {
        public readonly Team Team;

        private readonly Gamemode Gamemode;

        public ServerSetting<bool> Mortality { get; set; }

        public ServerSetting<bool> CanUseGuns { get; set; }

        public ToggleServerSetting<float> Vitality { get; set; }

        public ToggleServerSetting<float> Speed { get; set; }

        public ToggleServerSetting<float> Agility { get; set; }

        public ToggleServerSetting<float> StrengthUpper { get; set; }

        private TeamMetadata _SyncWith;

        public TeamMetadata(Team team, Gamemode gamemode, TeamSettings? config = null)
        {
            Gamemode = gamemode;
            Team = team;

            Mortality = CreateSetting(nameof(Mortality), config?.Mortality ?? default, nameof(Mortality));
            CanUseGuns = CreateSetting(nameof(CanUseGuns), config?.CanUseGuns ?? default, "Can Use Guns");

            Vitality = CreateSetting(nameof(Vitality), config?.Vitality, nameof(Vitality));
            Speed = CreateSetting(nameof(Speed), config?.Speed, nameof(Speed));
            Agility = CreateSetting(nameof(Agility), config?.Agility, nameof(Agility));
            StrengthUpper = CreateSetting(nameof(StrengthUpper), config?.StrengthUpper, "Strength Upper");
        }

        public new void Sync()
        {
            base.Sync();
            UpdateMenu();
        }

        public void SyncWith(TeamMetadata other)
        {
            _SyncWith = other;
            _SyncWith.OnSettingChanged += SettingChanged;
            _SyncWith.OnSettingSynced += ApplyConfig;
        }

        public void StopSync()
        {
            if (_SyncWith != null)
            {
                _SyncWith.OnSettingChanged -= SettingChanged;
                _SyncWith.OnSettingSynced -= ApplyConfig;
                _SyncWith = null;
            }
        }

        internal void SettingChanged()
        {
            Mortality.Value = _SyncWith.Mortality.Value;
            CanUseGuns.Value = _SyncWith.CanUseGuns.Value;

            Vitality.Value = _SyncWith.Vitality.Value;
            Vitality.Enabled = _SyncWith.Vitality.Enabled;

            Speed.Value = _SyncWith.Speed.Value;
            Speed.Enabled = _SyncWith.Speed.Enabled;

            Agility.Value = _SyncWith.Agility.Value;
            Agility.Enabled = _SyncWith.Agility.Enabled;

            StrengthUpper.Value = _SyncWith.StrengthUpper.Value;
            StrengthUpper.Enabled = _SyncWith.StrengthUpper.Enabled;

            UpdateMenu();
        }

        private void UpdateMenu()
        {
            if (!Gamemode.IsStarted)
                return;

            var team = Infection.Instance.TeamManager.GetInfectionTeamFromTeam(Team);

            if (team == null)
                return;

            GamemodeMenuManager.FormatApplyName(team, true);
        }

        private ToggleServerSetting<T> CreateSetting<T>(string name, ToggleSetting<T> config, string displayName = null) where T : IEquatable<T>
        {
            T _value;
            if (config == null)
                _value = default;
            else
                _value = config.Value ?? default;

            return CreateToggleServerSetting($"{Team.TeamName}_{name}", _value, config?.Enabled ?? default, displayName, false);
        }

        private ServerSetting<T> CreateSetting<T>(string name, T value, string displayName = null) where T : IEquatable<T>
            => CreateServerSetting($"{Team.TeamName}_{name}", value, displayName, false);

        public void ApplyConfig()
        {
            if (!NetworkInfo.IsHost)
                return;

            _settingsList.ForEach(setting =>
            {
                if (setting.IsServerSetting())
                    ((IServerSetting)setting).Sync();
            });
        }

        public bool IsApplied
        {
            get
            {
                if (!Gamemode.IsStarted)
                    return true;

                return _settingsList.TrueForAll(x => !x.IsServerSetting() || ((IServerSetting)x).IsSynced);
            }
        }

        static void CheckForGun(Hand hand)
        {
            var gun = hand.m_CurrentAttachedGO?.GetComponent<Gun>() ?? hand.m_CurrentAttachedGO?.GetComponentInParent<Gun>();
            if (gun != null)
                hand.TryDetach();
        }

        internal void CanUseGunsChanged()
        {
            if (Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.TeamManager.GetInfectionTeamFromTeam(Team) && !CanUseGuns.Value)
            {
                CheckForGun(Player.LeftHand);
                CheckForGun(Player.RightHand);
            }
        }
    }
}