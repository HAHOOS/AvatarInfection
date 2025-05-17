namespace AvatarInfection.Settings
{
    public interface ISetting
    {
        public string Name { get; }

        public void Save();

        public void Load();
    }
}