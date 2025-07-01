using System.Collections.Generic;

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

        public ServerSetting<bool> Mortality;

        public ServerSetting<bool> CanUseGuns;

        public ToggleServerSetting<float> Vitality;

        public ToggleServerSetting<float> Speed;

        public ToggleServerSetting<float> JumpPower;

        public ToggleServerSetting<float> Agility;

        public ToggleServerSetting<float> StrengthUpper;

        public TeamMetadata(Team team, Gamemode gamemode, TeamSettings? config = null)
        {
            Gamemode = gamemode;
            Team = team;
            Mortality = CreateServerSetting(
                $"{team.TeamName}_{nameof(Mortality)}",
                (config?.Mortality) ?? default, false);
            CanUseGuns = CreateServerSetting(
                $"{team.TeamName}_{nameof(CanUseGuns)}",
                (config?.CanUseGuns) ?? default, false);
            Vitality = CreateToggleServerSetting(
                $"{team.TeamName}_{nameof(Vitality)}",
                (config?.Vitality) ?? default,
                (config?.Vitality_Enabled) ?? default, false);
            Speed = CreateToggleServerSetting(
                $"{team.TeamName}_{nameof(Speed)}",
                 (config?.Speed) ?? default,
                 (config?.Speed_Enabled) ?? default, false);
            JumpPower = CreateToggleServerSetting(
                $"{team.TeamName}_{nameof(JumpPower)}",
                (config?.JumpPower) ?? default,
                (config?.JumpPower_Enabled) ?? default, false);
            Agility = CreateToggleServerSetting(
                $"{team.TeamName}_{nameof(Agility)}",
                (config?.Agility) ?? default,
                (config?.Agility_Enabled) ?? default, false);
            StrengthUpper = CreateToggleServerSetting(
                $"{team.TeamName}_{nameof(StrengthUpper)}",
                (config?.StrengthUpper) ?? default,
                (config?.StrengthUpper_Enabled) ?? default, false);
        }

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

                bool synced = true;
                foreach (var setting in _settingsList)
                {
                    if (setting.IsServerSetting())
                    {
                        if (!((IServerSetting)setting).IsSynced)
                        {
                            synced = false;
                            break;
                        }
                    }
                }

                return synced;
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
            if (Infection.Instance.TeamManager.GetLocalTeam() == Team && !CanUseGuns.ClientValue)
            {
                CheckForGun(Player.LeftHand);
                CheckForGun(Player.RightHand);
            }
        }
    }
}