﻿using System;

using AvatarInfection.Utilities;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Modules;

namespace AvatarInfection
{
    public class FusionModule : Module
    {
        /// <inheritdoc cref="Module.Name"/>
        public override string Name => "Avatar Infection";

        /// <inheritdoc cref="Module.Author"/>
        public override string Author => "HAHOOS";

        /// <inheritdoc cref="Module.Color"/>
        public override ConsoleColor Color => ConsoleColor.Green;

        /// <inheritdoc cref="Module.Version"/>
        public override Version Version => Version.Parse(Core.Version);

        internal static ModuleLogger Logger { get; private set; }

        protected override void OnModuleRegistered()
        {
            base.OnModuleRegistered();
            Logger = LoggerInstance;
#if DEBUG
            LoggerInstance.Warn("This is a debug build, which is made in a way to make debugging a bit easier. Please make sure to set the configuration to 'Release' before releasing it to the public.");
#endif
            GamemodeRegistration.RegisterGamemode<Infection>();
            LabPresenceExtension.Init();
        }
    }
}