namespace AvatarInfection.Settings
{
    public struct TeamSettings
    {
        public bool Mortality { get; set; }

        public bool CanUseGuns { get; set; }

        public ToggleSetting<float> Vitality { get; set; }

        public ToggleSetting<float> Speed { get; set; }

        public ToggleSetting<float> JumpPower { get; set; }

        public ToggleSetting<float> StrengthUpper { get; set; }

        public ToggleSetting<float> Agility { get; set; }

        public TeamSettings()
        {
        }

        public TeamSettings(TeamSettings old)
        {
            Vitality = old.Vitality;
            Speed = old.Speed;
            JumpPower = old.JumpPower;
            Agility = old.Agility;
            Mortality = old.Mortality;
            StrengthUpper = old.StrengthUpper;
            CanUseGuns = old.CanUseGuns;
        }
    }

    public class ToggleSetting<T>(T value, bool enabled = true)
    {
        public T Value { get; set; } = value;

        public bool Enabled { get; set; } = enabled;
    }
}