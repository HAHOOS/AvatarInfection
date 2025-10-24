using LabFusion.SDK.Gamemodes;

namespace AvatarInfection.Settings
{
    public struct InfectionTeam
    {
        public Team Team { get; set; }

        public TeamMetadata Metadata { get; set; }

        public InfectionTeam()
        {
        }

        public InfectionTeam(Team team, TeamMetadata metadata)
        {
            Team = team;
            Metadata = metadata;
        }

        public InfectionTeam(Team team, Gamemode gamemode, TeamSettings? config = null)
        {
            Team = team;
            Metadata = new(team, gamemode, config);
        }

        public static implicit operator Team(InfectionTeam team) => team.Team;
    }
}