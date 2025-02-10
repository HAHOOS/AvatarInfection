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
            ImageConversion.LoadImage(texture2d, bytes.ToArray(), false);
            texture2d.name = "AvatarInfectionIcon";
            texture2d.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Icon = texture2d;

            ModuleManager.RegisterModule<FusionModule>();
            LoggerInstance.Msg("Initialized.");
            HarmonyInstance.PatchAll(typeof(GrabPatches));
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