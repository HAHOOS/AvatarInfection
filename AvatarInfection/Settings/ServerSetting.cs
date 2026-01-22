using System;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

using AvatarInfection.Utilities;
using LabFusion.Network;
using LabFusion.Utilities;
using MelonLoader;

namespace AvatarInfection.Settings
{
    public class ServerSetting<T> : IServerSetting where T : IEquatable<T>
    {
        private readonly Gamemode gamemode;

        public string Name { get; private set; }

        public string DisplayName { get; set; }

        public bool AutoSync { get; set; }

        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                if (AutoSync)
                    Sync();
                if (NetworkInfo.IsHost && AutoSync)
                    OnValueChanged?.Invoke();
            }
        }

        public MetadataVariableT<T> ServerValue { get; }

        private MelonPreferences_Entry<T> Entry { get; set; }

        public bool IsSynced
            => Value.Equals(ServerValue.GetValue());

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public event Action OnSynced;

        public void Sync()
        {
            ServerValue.SetValue(_value);

            if (NetworkInfo.IsHost)
                OnValueChanged?.Invoke();

            OnSynced?.Invoke();
        }

        public void Load()
            => Value = Entry.Value;

        public void Save()
            => Entry.Value = Value;

        private void InitEvent(string name)
        {
            Name = name;
            Entry = Core.Category.CreateEntry(name, Value);
            GamemodeManager.OnGamemodeStarted += OnGamemodeStarted;
            MultiplayerHooking.OnStartedServer += Sync;
            MultiplayerHooking.OnJoinedServer += RetrieveValues;
            MultiplayerHooking.OnTargetLevelLoaded += RetrieveValues;
            gamemode.Metadata.OnMetadataChanged += MetadataChanged;
        }

        private void OnGamemodeStarted()
        {
            if (NetworkInfo.IsHost)
                Sync();
        }

        private void RetrieveValues()
        {
            if (!NetworkInfo.HasServer || NetworkInfo.IsHost)
                return;

            _value = ServerValue.GetValue();
        }

        private void MetadataChanged(string key, string value)
        {
            if (NetworkInfo.IsHost)
                return;

            if (key == ServerValue.Key)
            {
                var old = _value;
                _value = ServerValue.GetValue();

                if (!old.Equals(_value))
                    OnValueChanged?.Invoke();
            }
        }

        public ServerSetting(Gamemode gamemode, string name, string displayName = null, bool autoSync = true)
        {
            displayName ??= name;
            DisplayName = displayName;
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            Value = default;
            InitEvent(name);
        }

        public ServerSetting(Gamemode gamemode, string name, T value, string displayName = null, bool autoSync = true)
        {
            displayName ??= name;
            DisplayName = displayName;
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            Value = value;
            InitEvent(name);
        }
    }

    public class ToggleServerSetting<T> : IServerSetting
    {
        private readonly Gamemode gamemode;

        public bool AutoSync { get; set; }

        public string Name { get; private set; }

        public string DisplayName { get; set; }

        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                if (AutoSync)
                    Sync();
            }
        }

        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (AutoSync)
                    Sync();
            }
        }

        public bool IsSynced
            => Value.Equals(ServerValue.GetValue())
                && Enabled == ServerValue.IsEnabled;

        private MelonPreferences_Entry<T> Entry { get; set; }
        private MelonPreferences_Entry<bool> EnabledEntry { get; set; }

        public ToggleMetadataVariableT<T> ServerValue { get; }

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public event Action OnSynced;

        public void Sync()
        {
            ServerValue.SetValue(_value);
            ServerValue.SetEnabled(_enabled);

            if (NetworkInfo.IsHost)
                OnValueChanged?.Invoke();

            OnSynced?.Invoke();
        }

        public void Load()
        {
            Value = Entry.Value;
            Enabled = EnabledEntry.Value;
        }

        public void Save()
        {
            Entry.Value = Value;
            EnabledEntry.Value = Enabled;
        }

        private void InitEvent(string name)
        {
            Name = name;
            Entry = Core.Category.CreateEntry(name, Value);
            EnabledEntry = Core.Category.CreateEntry($"{name}_Enabled", Enabled);
            GamemodeManager.OnGamemodeStarted += OnGamemodeStarted;
            MultiplayerHooking.OnStartedServer += Sync;
            MultiplayerHooking.OnJoinedServer += RetrieveValues;
            MultiplayerHooking.OnTargetLevelLoaded += RetrieveValues;
            gamemode.Metadata.OnMetadataChanged += MetadataChanged;
        }

        private void OnGamemodeStarted()
        {
            if (NetworkInfo.IsHost)
                Sync();
        }

        private void RetrieveValues()
        {
            if (!NetworkInfo.HasServer || NetworkInfo.IsHost)
                return;

            _value = ServerValue.GetValue();
            _enabled = ServerValue.IsEnabled;
        }

        private void MetadataChanged(string key, string value)
        {
            if (NetworkInfo.IsHost)
                return;

            if (key == ServerValue.Key)
            {
                var old = _value;
                _value = ServerValue.GetValue();

                if (!old.Equals(_value))
                    OnValueChanged?.Invoke();
            }
            else if (key == ServerValue.ToggledKey)
            {
                if (Enabled == ServerValue.IsEnabled)
                    return;

                _enabled = ServerValue.IsEnabled;

                OnValueChanged?.Invoke();
            }
        }

        public ToggleServerSetting(Gamemode gamemode, string name, string displayName = null, bool autoSync = true)
        {
            displayName ??= name;
            DisplayName = displayName;
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            Value = default;
            Enabled = default;
            InitEvent(name);
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, string displayName = null, bool autoSync = true)
        {
            displayName ??= name;
            DisplayName = displayName;
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            Value = value;
            Enabled = default;
            InitEvent(name);
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, bool enabled, string displayName = null, bool autoSync = true)
        {
            displayName ??= name;
            DisplayName = displayName;
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            Value = value;
            Enabled = enabled;
            InitEvent(name);
        }
    }
}