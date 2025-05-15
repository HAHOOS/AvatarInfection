using BoneLib;

using Il2CppSLZ.Marrow;

using LabFusion.Extensions;
using LabFusion.Network;
using LabFusion.SDK.Gamemodes;

namespace AvatarInfection
{
    public class TeamMetadata
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

        public TeamMetadata(Team team, Gamemode gamemode, TeamConfig? config = null)
        {
            Gamemode = gamemode;
            Team = team;
            Mortality = new ServerSetting<bool>(
                gamemode, $"{team.TeamName}_{nameof(Mortality)}",
                (config?.Mortality) ?? default, false);
            CanUseGuns = new ServerSetting<bool>(
                gamemode, $"{team.TeamName}_{nameof(CanUseGuns)}",
                (config?.CanUseGuns) ?? default, false);
            Vitality = new ToggleServerSetting<float>(
                gamemode, $"{team.TeamName}_{nameof(Vitality)}",
                (config?.Vitality) ?? default,
                (config?.Vitality_Enabled) ?? default, false);
            Speed = new ToggleServerSetting<float>(
                gamemode, $"{team.TeamName}_{nameof(Speed)}",
                 (config?.Speed) ?? default,
                 (config?.Speed_Enabled) ?? default, false);
            JumpPower = new ToggleServerSetting<float>(
                gamemode, $"{team.TeamName}_{nameof(JumpPower)}",
                (config?.JumpPower) ?? default,
                (config?.JumpPower_Enabled) ?? default, false);
            Agility = new ToggleServerSetting<float>(
                gamemode, $"{team.TeamName}_{nameof(Agility)}",
                (config?.Agility) ?? default,
                (config?.Agility_Enabled) ?? default, false);
            StrengthUpper = new ToggleServerSetting<float>(
                gamemode, $"{team.TeamName}_{nameof(StrengthUpper)}",
                (config?.StrengthUpper) ?? default,
                (config?.StrengthUpper_Enabled) ?? default, false);
        }

        public void Save()
        {
            Mortality.Load();
            CanUseGuns.Load();

            Speed.Load();
            JumpPower.Save();
            Agility.Save();
            StrengthUpper.Save();
        }

        public void Load()
        {
            Mortality.Save();
            CanUseGuns.Save();

            Speed.Save();
            JumpPower.Load();
            Agility.Load();
            StrengthUpper.Load();
        }

        public void ApplyConfig()
        {
            if (!NetworkInfo.IsServer)
                return;

            Mortality.Sync();

            Vitality.Sync();
            Speed.Sync();
            JumpPower.Sync();
            Agility.Sync();
            StrengthUpper.Sync();

            CanUseGuns.Sync();
        }

        public bool IsApplied
        {
            get
            {
                if (!Gamemode.IsStarted)
                    return true;

                if (Mortality.ClientValue != Mortality.ServerValue.GetValue()) return false;
                else if (CanUseGuns.ClientValue != CanUseGuns.ServerValue.GetValue()) return false;
                else if (Speed.ClientValue != Speed.ServerValue.GetValue()) return false;
                else if (Speed.ClientEnabled != Speed.ServerValue.IsEnabled) return false;
                else if (Agility.ClientValue != Agility.ServerValue.GetValue()) return false;
                else if (Agility.ClientEnabled != Agility.ServerValue.IsEnabled) return false;
                else if (StrengthUpper.ClientValue != StrengthUpper.ServerValue.GetValue()) return false;
                else if (StrengthUpper.ClientEnabled != StrengthUpper.ServerValue.IsEnabled) return false;
                else if (Vitality.ClientValue != Vitality.ServerValue.GetValue()) return false;
                else if (Vitality.ClientEnabled != Vitality.ServerValue.IsEnabled) return false;
                else if (JumpPower.ClientValue != JumpPower.ServerValue.GetValue()) return false;
                else if (JumpPower.ClientEnabled != JumpPower.ServerValue.IsEnabled) return false;
                else return true;
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