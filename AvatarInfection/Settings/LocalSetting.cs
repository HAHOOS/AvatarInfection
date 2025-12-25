using System;

using MelonLoader;

namespace AvatarInfection.Settings
{
    internal class LocalSetting<T> : ISetting
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke();
            }
        }

        public event Action OnValueChanged;

        private MelonPreferences_Entry<T> Entry { get; }

        public string Name { get; }

        public void Load()
            => Value = Entry.Value;

        public void Save()
            => Entry.Value = Value;

        public LocalSetting(string name)
        {
            Value = default;
            Name = name;
            Entry = Core.Category.CreateEntry<T>(name, default);
        }

        public LocalSetting(string name, T value)
        {
            Value = value;
            Name = name;
            Entry = Core.Category.CreateEntry(name, value);
        }
    }
}