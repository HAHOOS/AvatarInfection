using System;
using System.Collections.Generic;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

using AvatarInfection.Utilities;
using LabFusion.Network;
using LabFusion.Utilities;
using MelonLoader;

namespace AvatarInfection.Settings
{
    public class ServerSetting<T> : IServerSetting
    {
        private readonly Gamemode gamemode;

        public string Name { get; private set; }

        public bool AutoSync { get; set; }

        private T _clientValue;

        public T ClientValue
        {
            get => _clientValue;
            set
            {
                _clientValue = value;
                if (AutoSync)
                    Sync();
            }
        }

        public MetadataVariableT<T> ServerValue { get; }

        private MelonPreferences_Entry<T> Entry { get; set; }

        public bool IsSynced
            => EqualityComparer<T>.Default.Equals(ClientValue, ServerValue.GetValue());

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public void Sync()
            => ServerValue.SetValue(_clientValue);

        public void Load()
            => ClientValue = Entry.Value;

        public void Save()
            => Entry.Value = ClientValue;

        private void InitEvent(string name)
        {
            Name = name;
            Entry = Core.Category.CreateEntry(name, ClientValue);
            GamemodeManager.OnGamemodeStarted += () =>
            {
                if (GamemodeManager.ActiveGamemode == gamemode && NetworkInfo.IsServer)
                    Sync();
            };
            MultiplayerHooking.OnJoinServer += () => _clientValue = ServerValue.GetValue();
            gamemode.Metadata.OnMetadataChanged += (key, _) =>
            {
                if (key == ServerValue.Key)
                {
                    var value = ServerValue.GetValue();
                    if (EqualityComparer<T>.Default.Equals(ClientValue, value))
                        return;

                    _clientValue = ServerValue.GetValue();

                    OnValueChanged?.Invoke();
                }
            };
        }

        public ServerSetting(Gamemode gamemode, string name, bool autoSync = true)
        {
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            ClientValue = default;
            InitEvent(name);
        }

        public ServerSetting(Gamemode gamemode, string name, T value, bool autoSync = true)
        {
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            ClientValue = value;
            InitEvent(name);
        }
    }

    public class ToggleServerSetting<T> : IServerSetting
    {
        private readonly Gamemode gamemode;

        public bool AutoSync { get; set; }

        public string Name { get; private set; }

        private T _clientValue;

        public T ClientValue
        {
            get => _clientValue;
            set
            {
                _clientValue = value;
                if (AutoSync)
                    Sync();
            }
        }

        private bool _clientEnabled;

        public bool ClientEnabled
        {
            get => _clientEnabled;
            set
            {
                _clientEnabled = value;
                if (AutoSync)
                    Sync();
            }
        }

        public bool IsSynced
            => EqualityComparer<T>.Default.Equals(ClientValue, ServerValue.GetValue())
                && ClientEnabled == ServerValue.IsEnabled;

        private MelonPreferences_Entry<T> Entry { get; set; }
        private MelonPreferences_Entry<bool> EnabledEntry { get; set; }

        public ToggleMetadataVariableT<T> ServerValue { get; }

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public void Sync()
        {
            ServerValue.SetValue(_clientValue);
            ServerValue.SetEnabled(_clientEnabled);
        }

        public void Load()
        {
            ClientValue = Entry.Value;
            ClientEnabled = EnabledEntry.Value;
        }

        public void Save()
        {
            Entry.Value = ClientValue;
            EnabledEntry.Value = ClientEnabled;
        }

        private void InitEvent(string name)
        {
            Name = name;
            Entry = Core.Category.CreateEntry(name, ClientValue);
            EnabledEntry = Core.Category.CreateEntry($"{name}_Enabled", ClientEnabled);
            GamemodeManager.OnGamemodeStarted += () =>
            {
                if (GamemodeManager.ActiveGamemode == gamemode && NetworkInfo.IsServer)
                    Sync();
            };
            MultiplayerHooking.OnJoinServer += () =>
            {
                _clientValue = ServerValue.GetValue();
                _clientEnabled = ServerValue.IsEnabled;
            };
            gamemode.Metadata.OnMetadataChanged += (key, _) =>
            {
                if (key == ServerValue.Key)
                {
                    if (EqualityComparer<T>.Default.Equals(ClientValue, ServerValue.GetValue()))
                        return;

                    _clientValue = ServerValue.GetValue();

                    OnValueChanged?.Invoke();
                }
                else if (key == ServerValue.ToggledKey)
                {
                    if (ClientEnabled == ServerValue.IsEnabled)
                        return;

                    _clientEnabled = ServerValue.IsEnabled;

                    OnValueChanged?.Invoke();
                }
            };
        }

        public ToggleServerSetting(Gamemode gamemode, string name, bool autoSync = true)
        {
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            ClientValue = default;
            ClientEnabled = default;
            InitEvent(name);
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, bool autoSync = true)
        {
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            ClientValue = value;
            ClientEnabled = default;
            InitEvent(name);
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, bool enabled, bool autoSync = true)
        {
            AutoSync = autoSync;
            this.gamemode = gamemode;
            ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            ClientValue = value;
            ClientEnabled = enabled;
            InitEvent(name);
        }
    }
}