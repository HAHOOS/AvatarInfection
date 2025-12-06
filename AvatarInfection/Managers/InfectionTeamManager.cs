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

        public new void Register(Gamemode gamemode)
        {
            base.Register(gamemode);
            base.OnAssignedToTeam += OnAssigned;
            base.OnRemovedFromTeam += OnRemoved;
        }

        public new void Unregister()
        {
            base.Unregister();
            base.OnAssignedToTeam -= OnAssigned;
            base.OnRemovedFromTeam -= OnRemoved;
        }

        private void OnAssigned(PlayerID player, Team team)
        {
            var infectionTeam = GetInfectionTeamFromTeam(team);
            if (infectionTeam != null)
                OnAssignedToInfectedTeam?.Invoke(player, infectionTeam);
        }

        private void OnRemoved(PlayerID player, Team team)
        {
            var infectionTeam = GetInfectionTeamFromTeam(team);
            if (infectionTeam != null)
                OnRemovedFromInfectedTeam?.Invoke(player, infectionTeam);
        }

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
            => InfectedTeams.FirstOrDefault(x => x.Team.HasPlayer(player));


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