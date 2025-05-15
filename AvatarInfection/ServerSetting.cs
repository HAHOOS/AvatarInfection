using System;
using System.Collections.Generic;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

using AvatarInfection.Utilities;
using LabFusion.Network;
using LabFusion.Utilities;

namespace AvatarInfection
{
    public class ServerSetting<T>
    {
        private readonly Gamemode gamemode;

        public bool AutoSync { get; set; } = true;

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

        public MetadataVariableT<T> ServerValue;

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public void Sync()
            => ServerValue.SetValue(_clientValue);

        private void InitEvent()
        {
            GamemodeManager.OnGamemodeStarted += () =>
            {
                if (GamemodeManager.ActiveGamemode == gamemode && NetworkInfo.IsServer)
                    Sync();
            };
            MultiplayerHooking.OnJoinServer += () =>
            {
                _clientValue = ServerValue.GetValue();
            };
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
            this.AutoSync = autoSync;
            this.gamemode = gamemode;
            this.ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = default;
            InitEvent();
        }

        public ServerSetting(Gamemode gamemode, string name, T value, bool autoSync = true)
        {
            this.AutoSync = autoSync;
            this.gamemode = gamemode;
            this.ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = value;
            InitEvent();
        }
    }

    public class ToggleServerSetting<T>
    {
        private readonly Gamemode gamemode;

        public bool AutoSync { get; set; } = true;

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

        public ToggleMetadataVariableT<T> ServerValue;

        /// <summary>
        /// This gets only triggered when the client value is set to the new server value
        /// </summary>
        public event Action OnValueChanged;

        public void Sync()
        {
            ServerValue.SetValue(_clientValue);
            ServerValue.SetEnabled(_clientEnabled);
        }

        private void InitEvent()
        {
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
            this.AutoSync = autoSync;
            this.gamemode = gamemode;
            this.ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = default;
            this.ClientEnabled = default;
            InitEvent();
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, bool autoSync = true)
        {
            this.AutoSync = autoSync;
            this.gamemode = gamemode;
            this.ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = value;
            this.ClientEnabled = default;
            InitEvent();
        }

        public ToggleServerSetting(Gamemode gamemode, string name, T value, bool enabled, bool autoSync = true)
        {
            this.AutoSync = autoSync;
            this.gamemode = gamemode;
            this.ServerValue = new ToggleMetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = value;
            this.ClientEnabled = enabled;
            InitEvent();
        }
    }
}