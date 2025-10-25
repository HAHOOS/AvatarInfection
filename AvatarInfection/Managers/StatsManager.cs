using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
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

            if (Infection.Instance.TeamManager.GetLocalTeam() == null)
                ClearOverrides();
            else
                Internal_SetStats(Infection.Instance.GetInfectedTeam(Infection.Instance.TeamManager.GetLocalTeam())?.Metadata);
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

        private static void Internal_SetStats(TeamMetadata metadata)
        {
            if (metadata == null)
                return;

            // Push nametag updates
            FusionOverrides.ForceUpdateOverrides();

            float? jumpPower = GetToggleValue(metadata.JumpPower);
            float? speed = GetToggleValue(metadata.Speed);
            float? agility = GetToggleValue(metadata.Agility);
            float? strengthUpper = GetToggleValue(metadata.StrengthUpper);

            FusionPlayerExtended.SetOverrides(jumpPower, speed, agility, strengthUpper);

            // Force mortality
            LocalHealth.MortalityOverride = metadata.Mortality.ClientValue;

            if (metadata.Vitality.ClientEnabled)
                LocalHealth.VitalityOverride = metadata.Vitality.ClientValue;
        }

        internal static void RefreshStats(string teamName)
        {
            Team team = Infection.Instance.TeamManager.GetTeamByName(teamName);

            if (team != null && Infection.Instance.TeamManager.GetLocalTeam() == team)
                ApplyStats();
        }
    }
}