using System;

namespace AvatarInfection.Settings
{
    public interface IServerSetting : ISetting
    {
        public bool IsSynced { get; }

        public void Sync();

        public event Action OnSynced;
    }
}