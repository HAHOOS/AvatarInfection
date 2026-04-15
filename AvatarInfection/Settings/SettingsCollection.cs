using System;
using System.Linq;
using System.Collections.Generic;

namespace AvatarInfection.Settings
{
    public class SettingsCollection
    {
        internal readonly List<ISetting> _settingsList = [];

        public IReadOnlyCollection<ISetting> Settings => _settingsList.AsReadOnly();

        public event Action OnSettingChanged;

        public event Action OnSettingSynced;

        public void Save(bool toFile = true)
        {
            _settingsList.ForEach(x => x.Save());
            if (toFile)
                Core.Category.SaveToFile(false);
        }

        public ISetting this[string name]
            => _settingsList.Find(x => x.Name == name);

        public ISetting this[int index]
            => _settingsList[index];

        public void Load()
            => _settingsList.ForEach(x => x.Load());

        public void Sync()
            => _settingsList.ForEach(x =>
            {
                if (x.IsServerSetting())
                    ((IServerSetting)x).Sync();
            });

        internal ServerSetting<T> CreateServerSetting<T>(string name, T value, string displayName = null, bool autoSync = true, bool saveable = true, Action onValueChanged = null) where T : IEquatable<T>
        {
            var setting = new ServerSetting<T>(Infection.Instance, name, value, displayName, autoSync, saveable);
            setting.OnValueChanged += () => OnSettingChanged?.Invoke();
            setting.OnSynced += () => OnSettingSynced?.Invoke();
            if (onValueChanged != null)
                setting.OnValueChanged += onValueChanged;
            _settingsList.Add(setting);
            return setting;
        }

        internal LocalSetting<T> CreateLocalSetting<T>(string name, T value, bool saveable = true, Action onValueChanged = null)
        {
            var setting = new LocalSetting<T>(name, value, saveable);
            setting.OnValueChanged += () => OnSettingChanged?.Invoke();
            if (onValueChanged != null)
                setting.OnValueChanged += onValueChanged;
            _settingsList.Add(setting);
            return setting;
        }

        internal ToggleServerSetting<T> CreateToggleServerSetting<T>(string name, T value, bool enabled, string displayName = null, bool autoSync = true, bool saveable = true, Action onValueChanged = null) where T : IEquatable<T>
        {
            var setting = new ToggleServerSetting<T>(Infection.Instance, name, value, enabled, displayName, autoSync, saveable);
            setting.OnValueChanged += () => OnSettingChanged?.Invoke();
            setting.OnSynced += () => OnSettingSynced?.Invoke();
            if (onValueChanged != null)
                setting.OnValueChanged += onValueChanged;
            _settingsList.Add(setting);
            return setting;
        }

        internal AvatarSetting CreateAvatarSetting(string name, AvatarSelectMode value, bool enabled, bool autoSync = true, bool optional = false, string groupName = "", Action onValueChanged = null)
            => CreateAvatarSetting(name, new SelectedAvatarData(null, value), enabled, autoSync, optional, groupName, onValueChanged);

        internal AvatarSetting CreateAvatarSetting(string name, SelectedAvatarData value, bool enabled, bool autoSync = true, bool optional = false, string groupName = "", Action onValueChanged = null)
        {
            var setting = new AvatarSetting(Infection.Instance, name, value, enabled, null, autoSync);
            setting.OnValueChanged += () => OnSettingChanged?.Invoke();
            setting.OnSynced += () => OnSettingSynced?.Invoke();
            setting.Optional = optional;
            setting.GroupName = groupName;
            if (onValueChanged != null)
                setting.OnValueChanged += onValueChanged;
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