using AvatarInfection.Utilities;

using BoneLib;

using Il2CppSLZ.Marrow;

using LabFusion.Extensions;
using LabFusion.Network;
using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

namespace AvatarInfection
{
    public class TeamMetadata
    {
        public readonly Team Team;

        public TeamConfig Config;

        public MetadataBool Mortality;

        public MetadataBool CanUseGuns;

        public ToggleMetadataVariableT<float> Vitality;

        public ToggleMetadataVariableT<float> Speed;

        public ToggleMetadataVariableT<float> JumpPower;

        public ToggleMetadataVariableT<float> Agility;

        public ToggleMetadataVariableT<float> StrengthUpper;

        public TeamMetadata(Team team, NetworkMetadata metadata, TeamConfig config)
        {
            Team = team;
            Config = config;
            metadata.OnMetadataChanged += OnMetadataChanged;
            Mortality = new MetadataBool($"{team.TeamName}_{nameof(Mortality)}", metadata);
            CanUseGuns = new MetadataBool($"{team.TeamName}_{nameof(CanUseGuns)}", metadata);
            Vitality = new ToggleMetadataVariableT<float>($"{team.TeamName}_{nameof(Vitality)}", metadata);
            Speed = new ToggleMetadataVariableT<float>($"{team.TeamName}_{nameof(Speed)}", metadata);
            JumpPower = new ToggleMetadataVariableT<float>($"{team.TeamName}_{nameof(JumpPower)}", metadata);
            Agility = new ToggleMetadataVariableT<float>($"{team.TeamName}_{nameof(Agility)}", metadata);
            StrengthUpper = new ToggleMetadataVariableT<float>($"{team.TeamName}_{nameof(StrengthUpper)}", metadata);
        }

        public void ApplyConfig()
        {
            if (!NetworkInfo.IsServer)
                return;

            Mortality.SetValue(Config.Mortality);

            Vitality.SetValue(Config.Vitality);
            Speed.SetValue(Config.Speed);
            JumpPower.SetValue(Config.JumpPower);
            Agility.SetValue(Config.Agility);
            StrengthUpper.SetValue(Config.StrengthUpper);

            Vitality.SetEnabled(Config.Vitality_Enabled);
            Speed.SetEnabled(Config.Speed_Enabled);
            JumpPower.SetEnabled(Config.JumpPower_Enabled);
            Agility.SetEnabled(Config.Agility_Enabled);
            StrengthUpper.SetEnabled(Config.StrengthUpper_Enabled);

            CanUseGuns.SetValue(Config.CanUseGuns);
        }

        public void RefreshConfig(bool setStats = true)
        {
            Config.Mortality = Mortality.GetValue();
            Config.Vitality = Vitality.GetValue();
            Config.Speed = Speed.GetValue();
            Config.JumpPower = JumpPower.GetValue();
            Config.Agility = Agility.GetValue();
            Config.StrengthUpper = StrengthUpper.GetValue();

            Config.Vitality_Enabled = Vitality.IsEnabled;
            Config.Speed_Enabled = Speed.IsEnabled;
            Config.JumpPower_Enabled = JumpPower.IsEnabled;
            Config.Agility_Enabled = Agility.IsEnabled;
            Config.StrengthUpper_Enabled = StrengthUpper.IsEnabled;

            Config.CanUseGuns = CanUseGuns.GetValue();

            if (Infection.Instance.IsStarted && setStats)
                Infection.Instance.SetStats();
        }

        public TeamConfig GetConfigFromMetadata()
        {
            var config = new TeamConfig
            {
                Agility = Agility.GetValue(),
                Vitality = Vitality.GetValue(),
                Speed = Speed.GetValue(),
                JumpPower = JumpPower.GetValue(),
                StrengthUpper = StrengthUpper.GetValue(),
                CanUseGuns = CanUseGuns.GetValue(),
                Agility_Enabled = Agility.IsEnabled,
                Speed_Enabled = Speed.IsEnabled,
                Vitality_Enabled = Vitality.IsEnabled,
                StrengthUpper_Enabled = StrengthUpper.IsEnabled,
                JumpPower_Enabled = JumpPower.IsEnabled,
                Mortality = Mortality.GetValue()
            };

            return config;
        }

        static void CheckForGun(Hand hand)
        {
            var gun = hand.m_CurrentAttachedGO?.GetComponent<Gun>() ?? hand.m_CurrentAttachedGO?.GetComponentInParent<Gun>();
            if (gun != null)
                hand.TryDetach();
        }

        internal void CanUseGunsChanged()
        {
            if (Infection.TeamManager.GetLocalTeam() == Team && !Config.CanUseGuns)
            {
                CheckForGun(Player.LeftHand);
                CheckForGun(Player.RightHand);
            }
        }

        internal void OnMetadataChanged(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                return;

            if (!Infection.Instance.IsStarted)
                return;

            if (name == Mortality.Key)
            {
                Config.Mortality = Mortality.GetValue();
            }
            else if (name == Vitality.Key)
            {
                Config.Vitality = Vitality.GetValue();
            }
            else if (name == Speed.Key)
            {
                Config.Speed = Speed.GetValue();
            }
            else if (name == JumpPower.Key)
            {
                Config.JumpPower = JumpPower.GetValue();
            }
            else if (name == Agility.Key)
            {
                Config.Agility = Agility.GetValue();
            }
            else if (name == StrengthUpper.Key)
            {
                Config.StrengthUpper = StrengthUpper.GetValue();
            }
            else if (name == Vitality.ToggledKey)
            {
                Config.Vitality_Enabled = Vitality.IsEnabled;
            }
            else if (name == Speed.ToggledKey)
            {
                Config.Speed_Enabled = Speed.IsEnabled;
            }
            else if (name == JumpPower.ToggledKey)
            {
                Config.JumpPower_Enabled = JumpPower.IsEnabled;
            }
            else if (name == Agility.ToggledKey)
            {
                Config.Agility_Enabled = Agility.IsEnabled;
            }
            else if (name == StrengthUpper.ToggledKey)
            {
                Config.StrengthUpper_Enabled = StrengthUpper.IsEnabled;
            }
            else if (name == CanUseGuns.Key)
            {
                Config.CanUseGuns = CanUseGuns.GetValue();
                CanUseGunsChanged();
            }
        }
    }
}