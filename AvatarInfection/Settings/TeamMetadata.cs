using System;

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

        public ToggleServerSetting<float> JumpPower { get; set; }

        public ToggleServerSetting<float> Agility { get; set; }

        public ToggleServerSetting<float> StrengthUpper { get; set; }

        public TeamMetadata(Team team, Gamemode gamemode, TeamSettings? config = null)
        {
            Gamemode = gamemode;
            Team = team;

            Mortality = CreateSetting(nameof(Mortality), config?.Mortality ?? default);
            CanUseGuns = CreateSetting(nameof(CanUseGuns), config?.CanUseGuns ?? default);
            Vitality = CreateSetting(nameof(Vitality), config?.Vitality);
            Speed = CreateSetting(nameof(Speed), config?.Speed);
            JumpPower = CreateSetting(nameof(JumpPower), config?.JumpPower);
            Agility = CreateSetting(nameof(Agility), config?.Agility);
            StrengthUpper = CreateSetting(nameof(StrengthUpper), config?.StrengthUpper);
        }

        private ToggleServerSetting<T> CreateSetting<T>(string name, ToggleSetting<T> config) where T : IEquatable<T>
        {
            T _value;
            if (config == null)
                _value = default;
            else
                _value = config.Value ?? default;

            return CreateToggleServerSetting($"{Team.TeamName}_{name}", _value, config?.Enabled ?? default, false);
        }

        private ServerSetting<T> CreateSetting<T>(string name, T value) where T : IEquatable<T>
            => CreateServerSetting($"{Team.TeamName}_{name}", value, false);

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
            if (Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.TeamManager.GetInfectionTeamFromTeam(Team) && !CanUseGuns.ClientValue)
            {
                CheckForGun(Player.LeftHand);
                CheckForGun(Player.RightHand);
            }
        }
    }
}