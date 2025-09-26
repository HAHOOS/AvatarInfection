namespace AvatarInfection.Settings
{
    public struct TeamSettings
    {
        public bool Mortality { get; set; }

        public bool CanUseGuns { get; set; }

        public bool Vitality_Enabled { get; set; }

        public float Vitality { get; set; }

        public bool Speed_Enabled { get; set; }

        public float Speed { get; set; }

        public bool JumpPower_Enabled { get; set; }

        public float JumpPower { get; set; }

        public bool StrengthUpper_Enabled { get; set; }

        public float StrengthUpper { get; set; }

        public bool Agility_Enabled { get; set; }

        public float Agility { get; set; }

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