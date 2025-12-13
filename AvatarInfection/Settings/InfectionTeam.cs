using LabFusion.SDK.Gamemodes;

namespace AvatarInfection.Settings
{
    public class InfectionTeam
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

        public override bool Equals(object obj)
            => obj is not null && (ReferenceEquals(this, obj) || (obj is InfectionTeam team && Team == team.Team)
            || (obj is Team team1 && Team == team1)
            || (obj is TeamMetadata metadata && metadata.Team == Team));

        public override int GetHashCode()
            => Team.GetHashCode();
    }
}