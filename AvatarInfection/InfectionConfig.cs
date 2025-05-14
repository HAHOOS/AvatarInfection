using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;

using UnityEngine.Profiling.Memory.Experimental;

using static AvatarInfection.Infection;
using static Il2CppSLZ.Bonelab.Feedback_Audio;

namespace AvatarInfection
{
    internal class InfectionConfig
    {
        internal ServerSetting<bool> DisableSpawnGun { get; set; }

        internal ServerSetting<bool> DisableDevTools { get; set; }

        internal ServerSetting<string> SelectedAvatar;

        internal int TimeLimit { get; set; } = Defaults.TimeLimit;

        internal int InfectedCount { get; set; } = Defaults.InfectedCount;

        internal bool NoTimeLimit { get; set; } = Defaults.UntilAllFound;

        internal bool TeleportOnStart { get; set; } = Defaults.ShouldTeleportToHost;

        internal InfectTypeEnum InfectType { get; set; } = Defaults.InfectType;

        internal ServerSetting<bool> AllowKeepInventory { get; set; }

        internal ServerSetting<int> CountdownLength { get; set; }

        internal bool SuicideInfects { get; set; } = Defaults.SuicideInfects;

        internal int HoldTime { get; set; } = Defaults.HoldTime;

        internal ServerSetting<bool> TeleportOnEnd { get; set; }

        internal ServerSetting<bool> UseDeathmatchSpawns { get; set; }

        internal bool SyncWithInfected { get; set; } = Defaults.SyncWithInfected;

        internal AvatarSelectMode SelectMode { get; set; } = Defaults.SelectMode;

        internal ServerSetting<bool> ShowCountdownToAll { get; set; }

        internal InfectionConfig()
        {
            DisableDevTools = new(Instance, nameof(DisableDevTools), Defaults.DisableDevTools);
            DisableSpawnGun = new(Instance, nameof(DisableSpawnGun), Defaults.DisableSpawnGun);

            SelectedAvatar = new(Instance, nameof(SelectedAvatar), null);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            CountdownLength = new(Instance, nameof(CountdownLength), Defaults.CountdownLength);

            AllowKeepInventory = new(Instance, nameof(AllowKeepInventory), value: Defaults.AllowKeepInventory);

            TeleportOnEnd = new(Instance, nameof(TeleportOnEnd), Defaults.TeleportOnEnd);

            UseDeathmatchSpawns = new(Instance, nameof(UseDeathmatchSpawns), Defaults.UseDeathMatchSpawns);
            UseDeathmatchSpawns.OnValueChanged += () =>
            {
                if (UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(false);
                else
                    ClearDeathmatchSpawns();
            };

            ShowCountdownToAll = new(Instance, nameof(ShowCountdownToAll), Defaults.ShowCountdownToAll);
        }

        internal void SelectedPlayerOverride()
        {
            if (!Instance.IsStarted)
                return;

            if (Instance.TeamManager.GetLocalTeam() != Instance.Infected
                && Instance.TeamManager.GetLocalTeam() != Instance.InfectedChildren)
            {
                return;
            }

            SwapAvatar(SelectedAvatar.ClientValue);
        }
    }
}