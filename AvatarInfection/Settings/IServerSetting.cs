namespace AvatarInfection.Settings
{
    public interface IServerSetting : ISetting
    {
        public bool IsSynced { get; }

        public void Sync();
    }
}