namespace AvatarInfection.Settings
{
    public struct TeamSettings
    {
        public bool Mortality;

        public bool CanUseGuns;

        public bool Vitality_Enabled;

        public float Vitality;

        public bool Speed_Enabled;

        public float Speed;

        public bool JumpPower_Enabled;

        public float JumpPower;

        public bool StrengthUpper_Enabled;

        public float StrengthUpper;

        public bool Agility_Enabled;

        public float Agility;

        public TeamSettings()
        {
        }

        public TeamSettings(TeamSettings old)
        {
            Vitality_Enabled = old.Vitality_Enabled;
            Vitality = old.Vitality;
            Speed = old.Speed;
            Speed_Enabled = old.Speed_Enabled;
            JumpPower = old.JumpPower;
            JumpPower_Enabled = old.JumpPower_Enabled;
            Agility = old.Agility;
            Agility_Enabled = old.Agility_Enabled;
            Mortality = old.Mortality;
            StrengthUpper_Enabled = old.StrengthUpper_Enabled;
            StrengthUpper = old.StrengthUpper;
            CanUseGuns = old.CanUseGuns;
        }
    }
}