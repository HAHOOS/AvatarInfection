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

        public MelonPreferences_Entry<T> Entry { get; }

        public string Name { get; }

        public virtual void Load()
            => Value = Entry.Value;

        public virtual void Save()
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