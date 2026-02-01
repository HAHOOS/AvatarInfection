using System;
using System.IO;

using AvatarInfection.Helper;
using AvatarInfection.Patches;
using AvatarInfection.Utilities;

using BoneLib;

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

        public static Thunderstore Thunderstore { get; private set; }

        private static bool FirstLoad { get; set; } = true;

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
                Unregister("Exception occurred preventing GrabPatches from working", false);
                return;
            }

            Thunderstore = new Thunderstore($"{Constants.Name} / {Constants.Version} A BONELAB Mod");
            Thunderstore.BL_FetchPackage(Constants.Name, Constants.Author, Constants.Version, LoggerInstance);
            Hooking.OnLevelLoaded += OnLevelLoaded;

            LoggerInstance.Msg("Registering module");
            ModuleManager.RegisterModule<FusionModule>();
            LoggerInstance.Msg("Initialized.");
        }

        private static void OnLevelLoaded(LevelInfo info)
        {
            if (FirstLoad)
            {
                FirstLoad = false;
                Thunderstore.BL_SendNotification();
            }
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