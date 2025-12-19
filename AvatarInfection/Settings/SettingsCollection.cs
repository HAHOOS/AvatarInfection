using System;
using System.Collections.Generic;

namespace AvatarInfection.Settings
{
    public class SettingsCollection
    {
        internal readonly List<ISetting> _settingsList = [];

        public void Save(bool toFile = true)
        {
            _settingsList.ForEach(x => x.Save());
            if (toFile)
                Core.Category.SaveToFile();
        }

        public void Load()
            => _settingsList.ForEach(x => x.Load());

        public void Sync()
            => _settingsList.ForEach(x =>
            {
                if (x.IsServerSetting())
                    ((IServerSetting)x).Sync();
            });

        internal ServerSetting<T> CreateServerSetting<T>(string name, T value, string displayName = null, bool autoSync = true) where T : IEquatable<T>
        {
            var setting = new ServerSetting<T>(Infection.Instance, name, value, displayName, autoSync);
            _settingsList.Add(setting);
            return setting;
        }

        internal LocalSetting<T> CreateLocalSetting<T>(string name, T value)
        {
            var setting = new LocalSetting<T>(name, value);
            _settingsList.Add(setting);
            return setting;
        }

        internal ToggleServerSetting<T> CreateToggleServerSetting<T>(string name, T value, bool enabled, string displayName = null, bool autoSync = true) where T : IEquatable<T>
        {
            var setting = new ToggleServerSetting<T>(Infection.Instance, name, value, enabled, displayName, autoSync);
            _settingsList.Add(setting);
            return setting;
        }
    }

    public static class SettingsHelper
    {
        public static bool IsServerSetting(this ISetting setting)
            => setting is IServerSetting;
    }
}