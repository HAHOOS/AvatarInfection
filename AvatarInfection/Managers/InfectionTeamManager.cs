using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

namespace AvatarInfection.Managers
{
    internal class InfectionTeamManager : TeamManager
    {
        public List<InfectionTeam> InfectedTeams { get; } = [];

        public event Action<PlayerID, InfectionTeam> OnAssignedToInfectedTeam, OnRemovedFromInfectedTeam;

        private readonly Dictionary<byte, MetadataVariable> _playersToInfectedTeam = [];

        public new void Register(Gamemode gamemode)
        {
            base.Register(gamemode);

            gamemode.Metadata.OnMetadataChanged += OnMetadataChanged;
            gamemode.Metadata.OnMetadataRemoved += OnMetadataRemoved;
        }

        public new void Unregister()
        {
            base.Unregister();

            Gamemode.Metadata.OnMetadataChanged -= OnMetadataChanged;
            Gamemode.Metadata.OnMetadataRemoved -= OnMetadataRemoved;
        }

        private void OnMetadataChanged(string key, string value)
        {
            // Check if this is a team key
            if (!KeyHelper.KeyMatchesVariable(key, CommonKeys.TeamKey))
            {
                return;
            }

            var player = KeyHelper.GetPlayerFromKey(key);

            var playerID = PlayerIDManager.GetPlayerID(player);

            var teamVariable = new MetadataVariable(key, Gamemode.Metadata);

            _playersToInfectedTeam[player] = teamVariable;

            // Remove from existing teams
            foreach (var existingTeam in InfectedTeams)
            {
                if (!existingTeam.HasPlayer(player))
                {
                    continue;
                }

                if (existingTeam.HasPlayer(player))
                    existingTeam.ForceRemovePlayer(player);

                if (playerID != null)
                {
                    OnRemovedFromInfectedTeam?.Invoke(playerID, existingTeam);
                }
            }

            // Invoke team change event
            var team = GetTeamByName(value);

            if (team != null)
            {

                if (!team.HasPlayer(player))
                    team.ForceAddPlayer(player);

                if (playerID != null)
                {
                    OnAssignedToInfectedTeam?.Invoke(playerID, team);
                }
            }
        }

        private void OnMetadataRemoved(string key, string value)
        {
            // Check if this is a team key
            if (!KeyHelper.KeyMatchesVariable(key, CommonKeys.TeamKey))
            {
                return;
            }

            var player = KeyHelper.GetPlayerFromKey(key);

            var playerID = PlayerIDManager.GetPlayerID(player);

            _playersToInfectedTeam.Remove(player);

            // Invoke team remove event
            var team = GetTeamByName(value);

            if (team != null)
            {

                if (team.HasPlayer(player))
                    team.ForceRemovePlayer(player);

                if (playerID != null)
                {
                    OnRemovedFromInfectedTeam?.Invoke(playerID, team);
                }
            }
        }

        public void AddTeam(InfectionTeam team)
        {
            InfectedTeams.Add(team);
            base.AddTeam(team);
        }

        public void RemoveTeam(InfectionTeam team)
        {
            InfectedTeams.Remove(team);
            base.RemoveTeam(team);
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
                if (team.TeamName == name)
                {
                    return team;
                }
            }

            return null;
        }

        public new InfectionTeam GetPlayerTeam(PlayerID player)
            => InfectedTeams.FirstOrDefault(x => x.HasPlayer(player));


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
                if (team.PlayerCount < lowestPlayers)
                {
                    lowestPlayers = team.PlayerCount;
                    lowestTeam = team;
                }
            }

            return lowestTeam;
        }


        public InfectionTeam GetInfectionTeamFromTeam(Team team)
            => InfectedTeams.FirstOrDefault(x => x == team);
    }
}