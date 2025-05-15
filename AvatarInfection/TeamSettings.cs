using System.Diagnostics.CodeAnalysis;

namespace AvatarInfection
{
    public struct TeamSettings
    {
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

        public bool Mortality;

        public bool CanUseGuns;

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

        public override readonly bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is not TeamSettings config)
                return false;

            if (Vitality != config.Vitality)
                return false;

            if (Vitality_Enabled != config.Vitality_Enabled)
                return false;

            if (Speed != config.Speed)
                return false;

            if (Speed_Enabled != config.Speed_Enabled)
                return false;

            if (Agility != config.Agility)
                return false;

            if (Agility_Enabled != config.Agility_Enabled)
                return false;

            if (Mortality != config.Mortality)
                return false;

            if (CanUseGuns != config.CanUseGuns)
                return false;

            if (StrengthUpper != config.StrengthUpper)
                return false;

            if (StrengthUpper_Enabled != config.StrengthUpper_Enabled)
                return false;

            if (JumpPower_Enabled != config.JumpPower_Enabled)
                return false;

            if (JumpPower != config.JumpPower)
                return false;

            return true;
        }

        public override readonly int GetHashCode()
        {
            return Vitality.GetHashCode() + Vitality_Enabled.GetHashCode() +
                   Agility.GetHashCode() + Agility_Enabled.GetHashCode() +
                   Speed.GetHashCode() + Speed_Enabled.GetHashCode() +
                   StrengthUpper.GetHashCode() + StrengthUpper_Enabled.GetHashCode() +
                   JumpPower.GetHashCode() + JumpPower_Enabled.GetHashCode() +
                   Mortality.GetHashCode() + CanUseGuns.GetHashCode();
        }

        public static bool operator ==(TeamSettings left, TeamSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TeamSettings left, TeamSettings right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return @$"==========================================
Vitality: {Vitality} (Enabled: {Vitality_Enabled})
Speed: {Speed} (Enabled: {Speed_Enabled})
Jump Power: {JumpPower} (Enabled: {JumpPower_Enabled})
Agility: {Agility} (Enabled: {Agility_Enabled})
Strength Upper: {StrengthUpper} (Enabled: {StrengthUpper_Enabled})

Mortality: {Mortality}
Can Use Guns: {CanUseGuns}
==========================================";
        }
    }
}