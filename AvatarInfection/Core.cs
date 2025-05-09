using System;
using System.Collections.Generic;
using System.Reflection;

using AvatarInfection.Helper;
using AvatarInfection.Patches;
using AvatarInfection.Utilities;

using LabFusion.SDK.Modules;

using MelonLoader;

using UnityEngine;

namespace AvatarInfection
{
    public class Core : MelonMod
    {
        public const string Version = "1.0.0";

        public static MelonLogger.Instance Logger { get; private set; }

        public static Texture2D Icon { get; private set; }

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            try
            {
                LoggerInstance.Msg("Loading icon");
                var assembly = Assembly.GetExecutingAssembly();
                var name = assembly.GetName().Name;
                var path = $"{name}.Embedded.Icons.AvatarInfection.png";
                var stream = assembly.GetManifestResourceStream(path);
                List<byte> bytes = [];
                stream.Position = 0;
                while (true)
                {
                    var _byte = stream.ReadByte();
                    if (_byte == -1)
                        break;

                    bytes.Add((byte)_byte);
                }
                var texture2d = new Texture2D(2, 2);
                texture2d.LoadImage(bytes.ToArray(), false);
                texture2d.name = "AvatarInfectionIcon";
                texture2d.hideFlags = HideFlags.DontUnloadUnusedAsset;
                Icon = texture2d;
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