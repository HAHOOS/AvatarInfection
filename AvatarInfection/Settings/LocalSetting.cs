using MelonLoader;

namespace AvatarInfection.Settings
{
    internal class LocalSetting<T> : ISetting
    {
        public T Value { get; set; }

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