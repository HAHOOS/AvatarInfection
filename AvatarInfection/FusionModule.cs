using System;

using AvatarInfection.Utilities;

using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Modules;

namespace AvatarInfection
{
    public class FusionModule : Module
    {
        /// <inheritdoc cref="Module.Name"/>
        public override string Name => Constants.Name;

        /// <inheritdoc cref="Module.Author"/>
        public override string Author => Constants.Author;

        /// <inheritdoc cref="Module.Color"/>
        public override ConsoleColor Color => ConsoleColor.Green;

        /// <inheritdoc cref="Module.Version"/>
        public override Version Version => Version.Parse(Constants.Version);

        internal static ModuleLogger Logger { get; private set; }

        protected override void OnModuleRegistered()
        {
            base.OnModuleRegistered();
            Logger = LoggerInstance;
#if DEBUG || SOLOTESTING
            LoggerInstance.Warn("This is a debug build, which is made in a way to make debugging a bit easier. Please make sure to set the configuration to 'Release' before releasing it to the public.");
            LoggerInstance.Warn("If you have downloaded the mod from Thunderstore / Github and you are receiving this warning, please contact the mod author. You can do that by DMing @hahoos on Discord or creating an issue on Github.");
            LoggerInstance.Warn("Debug builds may have some features disabled or altered for testing purposes.");
#endif
            GamemodeRegistration.RegisterGamemode<Infection>();
            LabPresenceExtension.Init();
        }
    }
}