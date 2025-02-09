using AvatarInfection.Patches;
using AvatarInfection.Utilities;

using LabFusion.SDK.Modules;

using MelonLoader;

namespace AvatarInfection
{
    public class Core : MelonMod
    {
        public const string Version = "1.0.0";

        public static MelonLogger.Instance Logger { get; private set; }

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

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