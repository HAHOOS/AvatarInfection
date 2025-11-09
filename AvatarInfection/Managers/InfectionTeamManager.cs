using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using LabFusion.Player;
using LabFusion.SDK.Gamemodes;

namespace AvatarInfection.Managers
{
    internal class InfectionTeamManager : TeamManager
    {
        public List<InfectionTeam> InfectedTeams { get; } = [];

        public event Action<PlayerID, InfectionTeam> OnAssignedToInfectedTeam, OnRemovedFromInfectedTeam;

        public void AddTeam(InfectionTeam team)
        {
            InfectedTeams.Add(team);
            base.AddTeam(team.Team);
        }

        public void RemoveTeam(InfectionTeam team)
        {
            InfectedTeams.Remove(team);
            base.RemoveTeam(team.Team);
        }

        public new void ClearTeams()
        {
            UnassignAllPlayers();

            foreach (var team in InfectedTeams.ToArray())
                RemoveTeam(team);
        }

        public new InfectionTeam GetTeamByName(string name)
        {
            foreach (var team in InfectedTeams)
            {
                if (team.Team.TeamName == name)
                {
                    return team;
                }
            }

            return null;
        }

        public new InfectionTeam GetPlayerTeam(PlayerID player)
        {
            foreach (var team in InfectedTeams)
            {
                if (team.Team.HasPlayer(player))
                    return team;
            }
            return null;
        }

        public new InfectionTeam GetLocalTeam()
        {
            return GetPlayerTeam(PlayerIDManager.LocalID);
        }

        public new InfectionTeam GetRandomTeam()
        {
            return InfectedTeams.Random();
        }

        public new InfectionTeam GetTeamWithFewestPlayers()
        {
            int lowestPlayers = int.MaxValue;
            InfectionTeam lowestTeam = null;

            foreach (var team in InfectedTeams)
            {
                if (team.Team.PlayerCount < lowestPlayers)
                {
                    lowestPlayers = team.Team.PlayerCount;
                    lowestTeam = team;
                }
            }

            return lowestTeam;
        }

        public InfectionTeam GetInfectionTeamFromTeam(Team team)
            => InfectedTeams.FirstOrDefault(x => x.Team == team);
    }
}