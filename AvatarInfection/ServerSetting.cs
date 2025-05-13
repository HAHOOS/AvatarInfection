using System;
using System.Collections.Generic;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

namespace AvatarInfection
{
    internal class ServerSetting<T>
    {
        private readonly Gamemode gamemode;
        private readonly string name;

        private T _clientValue;

        public T ClientValue
        {
            get => _clientValue;
            set
            {
                _clientValue = value;
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

        private string GetName() => "ServerSetting_" + name;

        private void InitEvent()
        {
            GamemodeManager.OnGamemodeStarted += () =>
            {
                if (GamemodeManager.ActiveGamemode == gamemode)
                    Sync();
            };
            gamemode.Metadata.OnMetadataChanged += (key, _) =>
            {
                if (key == GetName())
                {
                    var value = ServerValue.GetValue();
                    if (EqualityComparer<T>.Default.Equals(ClientValue, value))
                        return;

                    OnValueChanged?.Invoke();
                }
            };
        }

        public ServerSetting(Gamemode gamemode, string name)
        {
            this.name = name;
            this.gamemode = gamemode;
            this.ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = default;
            InitEvent();
        }

        public ServerSetting(Gamemode gamemode, string name, T value)
        {
            this.name = name;
            this.gamemode = gamemode;
            this.ServerValue = new MetadataVariableT<T>("ServerSetting_" + name, gamemode.Metadata);
            this.ClientValue = value;
            InitEvent();
        }
    }
}