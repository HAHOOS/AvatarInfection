using System;
using System.IO;

using AvatarInfection.Helper;
using AvatarInfection.Patches;
using AvatarInfection.Utilities;

using LabFusion.SDK.Modules;

using MelonLoader;
using MelonLoader.Utils;

using UnityEngine;

namespace AvatarInfection
{
    public class Core : MelonMod
    {
        public static MelonLogger.Instance Logger { get; private set; }

        public static Texture2D Icon { get; private set; }

        public static MelonPreferences_Category Category { get; private set; }

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            Category = MelonPreferences.CreateCategory("AvatarInfection_Save");
            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "AvatarInfection.cfg"));

            try
            {
                LoggerInstance.Msg("Loading icon");
                Icon = ImageConversion.LoadTexture("AvatarInfectionIcon", "AvatarInfection.png");
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Failed to load icon, exception:\n{e}");
            }

            try
            {
                LoggerInstance.Msg("Patching");
                HarmonyInstance.PatchAll(typeof(GrabPatches));
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Failed to patch methods related to grab, exception:\n{e}");
                LoggerInstance.Error("To ensure fair play, the mod will be unloaded. Grab patches failing will cause some of the restrictions to not work properly. If you repeatedly get this issue, please create an issue on https://github.com/HAHOOS/AvatarInfection");
                this.Unregister("Exception occurred preventing GrabPatches from working", false);
                return;
            }

            LoggerInstance.Msg("Registering module");
            ModuleManager.RegisterModule<FusionModule>();
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnFixedUpdate()
        {
            TimeUtilities.OnEarlyFixedUpdate();
        }

        public override void OnUpdate()
        {
            TimeUtilities.OnEarlyUpdate();
            GrabPatches.Update();
        }
    }
}