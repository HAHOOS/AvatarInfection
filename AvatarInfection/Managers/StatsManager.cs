using System;

using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using LabFusion.Player;
using LabFusion.Utilities;

namespace AvatarInfection.Managers
{
    public static class StatsManager
    {
        public static void ClearOverrides()
        {
            // Reset mortality
            LocalHealth.MortalityOverride = null;
            LocalHealth.VitalityOverride = null;

            FusionPlayerExtended.ClearAllOverrides();
            FusionPlayerExtended.ClearAvatarOverride();
        }

        internal static void ApplyStats()
        {
            if (!Infection.Instance.IsStarted)
                return;

            // why the fuck does this return null
            var team = Infection.Instance.TeamManager.GetLocalTeam();
            if (team == null)
                ClearOverrides();
            else
                Internal_SetStats(team);
        }

        private static T? GetToggleValue<T>(ToggleServerSetting<T> serverSetting) where T : struct, IEquatable<T>
        {
            if (serverSetting == null)
                return null;

            if (serverSetting.ClientEnabled)
                return serverSetting.ClientValue;
            else
                return null;
        }

        private static void Internal_SetStats(InfectionTeam team)
        {
            var metadata = team.Metadata;
            if (metadata == null)
                return;

            FusionOverrides.ForceUpdateOverrides();

            float? speed = GetToggleValue(metadata.Speed);
            float? agility = GetToggleValue(metadata.Agility);
            float? strengthUpper = GetToggleValue(metadata.StrengthUpper);

            FusionPlayerExtended.SetOverrides(speed, agility, strengthUpper);

            LocalHealth.MortalityOverride = metadata.Mortality.ClientValue;

            if (metadata.Vitality.ClientEnabled)
                LocalHealth.VitalityOverride = metadata.Vitality.ClientValue;
        }

        internal static void RefreshStats(string teamName)
        {
            var team = Infection.Instance.TeamManager.GetTeamByName(teamName);

            if (team != null && Infection.Instance.TeamManager.GetLocalTeam() == team)
                ApplyStats();
        }
    }
}