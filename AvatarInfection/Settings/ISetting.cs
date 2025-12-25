using System;

namespace AvatarInfection.Settings
{
    public interface ISetting
    {
        public string Name { get; }

        public event Action OnValueChanged;

        public void Save();

        public void Load();
    }
}