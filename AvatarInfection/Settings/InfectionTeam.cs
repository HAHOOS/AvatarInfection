using System;

using LabFusion.SDK.Gamemodes;

using UnityEngine;

namespace AvatarInfection.Settings
{
    public class InfectionTeam : Team
    {
        public TeamMetadata StaticMetadata { get; set; }

        public TeamMetadata Metadata
        {
            get => Func != null ? Func.Invoke() : StaticMetadata;
            set
            {
                StaticMetadata = value;
                Func = () => value;
            }
        }

        public Color Color { get; set; }

        public Func<TeamMetadata> Func { get; set; }

        public InfectionTeam(string name) : base(name)
        {
        }

        public InfectionTeam(string name, Color color, TeamMetadata metadata) : base(name)
        {
            Color = color;
            Metadata = metadata;
        }

        public InfectionTeam(string name, Color color, Func<TeamMetadata> function) : base(name)
        {
            Color = color;
            Func = function;
        }

        public InfectionTeam(string name, Color color, TeamMetadata metadata, Func<TeamMetadata> function) : base(name)
        {
            Color = color;
            StaticMetadata = metadata;
            Func = function;
        }

        public InfectionTeam(string name, Color color, Gamemode gamemode, TeamSettings? config = null, Func<TeamMetadata> function = null) : base(name)
        {
            Color = color;
            StaticMetadata = new(this, gamemode, config);
            Func = function;
        }

        public override bool Equals(object obj)
            => obj is not null && (ReferenceEquals(this, obj) || (obj is InfectionTeam team && this == team)
            || (obj is Team team1 && this == team1)
            || (obj is TeamMetadata metadata && metadata.Team == this));

        public override int GetHashCode()
            => this.TeamName.GetHashCode() + this.PlayerCount.GetHashCode() + this.Color.GetHashCode();
    }
}