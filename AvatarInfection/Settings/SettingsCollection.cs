using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvatarInfection.Settings
{
    public class SettingsCollection
    {
        internal readonly List<ISetting> _settingsList = [];

        public void Save()
        {
            _settingsList.ForEach(x => x.Save());
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

        internal ServerSetting<T> CreateServerSetting<T>(string name, T value, bool autoSync = true)
        {
            var setting = new ServerSetting<T>(Infection.Instance, name, value, autoSync);
            _settingsList.Add(setting);
            return setting;
        }

        internal LocalSetting<T> CreateLocalSetting<T>(string name, T value)
        {
            var setting = new LocalSetting<T>(name, value);
            _settingsList.Add(setting);
            return setting;
        }

        internal ToggleServerSetting<T> CreateToggleServerSetting<T>(string name, T value, bool enabled, bool autoSync = true)
        {
            var setting = new ToggleServerSetting<T>(Infection.Instance, name, value, enabled, autoSync);
            _settingsList.Add(setting);
            return setting;
        }
    }

    public static class SettingsHelper
    {
        public static bool IsServerSetting(this ISetting setting)
             => typeof(IServerSetting).IsAssignableFrom(setting.GetType());
    }
}