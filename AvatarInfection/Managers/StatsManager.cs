using System;

using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using LabFusion.Utilities;

namespace AvatarInfection.Managers
{
    public static class StatsManager
    {
        public static void ClearOverrides()
        {
            Overrides.ClearAllOverrides();
            Overrides.ClearAvatarOverride();
        }

        internal static void ApplyStats()
        {
            if (!Infection.Instance.IsStarted)
                return;

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

            if (serverSetting.Enabled)
                return serverSetting.Value;
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
            float? vitality = GetToggleValue(metadata.Vitality);

            Overrides.SetOverrides(speed, agility, strengthUpper, vitality, metadata.Mortality.Value);
        }

        internal static void RefreshStats(string teamName)
        {
            var team = Infection.Instance.TeamManager.GetTeamByName(teamName);

            if (team != null && Infection.Instance.TeamManager.GetLocalTeam() == team)
                ApplyStats();
        }
    }
}