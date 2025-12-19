using LabFusion.SDK.Gamemodes;

using UnityEngine;

namespace AvatarInfection.Settings
{
    public class InfectionTeam : Team
    {

        public TeamMetadata Metadata { get; set; }

        public Color Color { get; set; }

        public InfectionTeam(string name) : base(name)
        {
        }

        public InfectionTeam(string name, Color color, TeamMetadata metadata) : base(name)
        {
            Color = color;
            Metadata = metadata;
        }

        public InfectionTeam(string name, Color color, Gamemode gamemode, TeamSettings? config = null) : base(name)
        {
            Color = color;
            Metadata = new(this, gamemode, config);
        }

        public override bool Equals(object obj)
            => obj is not null && (ReferenceEquals(this, obj) || (obj is InfectionTeam team && this == team)
            || (obj is Team team1 && this == team1)
            || (obj is TeamMetadata metadata && metadata.Team == this));
    }
}