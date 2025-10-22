// Ignore Spelling: Metadata Unragdoll

using System;
using System.Collections.Generic;
using System.Collections;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Bonelab;

using LabFusion.Menu.Data;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;
using LabFusion.Entities;
using LabFusion.SDK.Modules;
using LabFusion.Network;
using LabFusion.Extensions;
using LabFusion.SDK.Points;
using LabFusion.Marrow;
using LabFusion.Scene;
using LabFusion.Senders;
using LabFusion.Marrow.Integration;
using LabFusion.SDK.Metadata;
using LabFusion.RPC;
using LabFusion.Preferences.Client;
using LabFusion.Downloading;
using LabFusion.Data;
using LabFusion.UI.Popups;
using LabFusion.Bonelab;

using UnityEngine;

using MelonLoader;

using BoneLib;

using AvatarInfection.Utilities;
using AvatarInfection.Helper;
using AvatarInfection.Managers;
using AvatarInfection.Settings;

namespace AvatarInfection
{
    public class Infection : Gamemode
    {
        public override string Title => "Avatar Infection";

        public override string Author => "HAHOOS";

        public override string Barcode => Defaults.Barcode;

        public override string Description => "An infection is spreading, turning people into a selected avatar by the host.";

        public override Texture Logo => Core.Icon;

        public override bool DisableSpawnGun => Config?.DisableSpawnGun?.ClientValue ?? true;

        public override bool DisableDevTools => Config?.DisableDevTools?.ClientValue ?? true;

        public override bool AutoHolsterOnDeath => true;

        public override bool DisableManualUnragdoll => true;

        public override bool AutoStopOnSceneLoad => true;

        public override bool ManualReady => false;

        public static Infection Instance { get; private set; }

        internal Team Infected { get; } = new("Infected");

        internal TeamMetadata InfectedMetadata;

        internal Team Survivors { get; } = new("Survivors");

        internal TeamMetadata SurvivorsMetadata;

        internal Team InfectedChildren { get; } = new("InfectedChildren");

        internal TeamMetadata InfectedChildrenMetadata;

        internal TeamManager TeamManager { get; } = new();

        internal static class Defaults
        {
            public const string Barcode = "HAHOOS.AvatarInfection";

            public const int TimeLimit = 10;

            public const int InfectedBitReward = 50;

            public const int SurvivorsBitReward = 75;

            public const bool NoTimeLimit = false;

            public const bool DontShowAnyNameTags = false;

            public readonly static TeamSettings InfectedStats = new()
            {
                Vitality = 0.75f,
                JumpPower = 1.5f,
                Speed = 2.8f,
                Agility = 2f,
                StrengthUpper = 0.5f,

                JumpPower_Enabled = true,
                Speed_Enabled = true,
                Vitality_Enabled = true,
                Agility_Enabled = true,
                StrengthUpper_Enabled = true,

                Mortality = true,

                CanUseGuns = false,
            };

            public readonly static TeamSettings InfectedChildrenStats = new()
            {
                Vitality = 0.5f,
                JumpPower = 1.25f,
                Speed = 1.8f,
                Agility = 1.5f,
                StrengthUpper = 0.35f,

                JumpPower_Enabled = true,
                Speed_Enabled = true,
                Vitality_Enabled = true,
                Agility_Enabled = true,
                StrengthUpper_Enabled = true,

                Mortality = true,

                CanUseGuns = false,
            };

            public readonly static TeamSettings SurvivorsStats = new()
            {
                Vitality = 1f,
                JumpPower = 1f,
                Speed = 1.2f,
                Agility = 1f,
                StrengthUpper = 1.5f,

                JumpPower_Enabled = true,
                Speed_Enabled = true,
                Vitality_Enabled = true,
                Agility_Enabled = true,
                StrengthUpper_Enabled = true,

                Mortality = true,

                CanUseGuns = true,
            };

            public const bool DisableSpawnGun = true;

            public const bool DisableDevTools = true;

            public const bool AllowKeepInventory = false;

            public const int InfectedCount = 1;

            public const bool TeleportOnStart = true;

            public const int CountdownLength = 30;

            public const InfectType _InfectType = InfectType.TOUCH;

            public const bool SuicideInfects = true;

            public const int HoldTime = 0;

            public const bool TeleportOnEnd = false;

            public const bool UseDeathMatchSpawns = true;

            public const bool SyncWithInfected = false;

            public const bool ShowCountdownToAll = false;

            public const AvatarSelectMode SelectMode = AvatarSelectMode.CONFIG;

            // Have no idea for mono discs
            public static readonly MonoDiscReference[] Tracks =
        [
            BonelabMonoDiscReferences.TheRecurringDreamReference,
            BonelabMonoDiscReferences.HeavyStepsReference,
            BonelabMonoDiscReferences.StankFaceReference,
            BonelabMonoDiscReferences.AlexInWonderlandReference,
            BonelabMonoDiscReferences.ItDoBeGroovinReference,

            BonelabMonoDiscReferences.ConcreteCryptReference, // concrete crypt
        ];
        }

        public MusicPlaylist PlayList { get; } = new();

        internal MetadataBool InfectedLooking { get; private set; }

        internal bool InitialTeam { get; private set; } = true;

        public bool HasBeenInfected { get; private set; } = false;

        private static ModuleLogger Logger => FusionModule.Logger;

        private float _elapsedTime = 0f;

        public float ElapsedSeconds => _elapsedTime;
        public int ElapsedMinutes => Mathf.FloorToInt(ElapsedSeconds / 60f);

        internal MetadataInt CountdownValue { get; private set; }

        internal MetadataVariableT<long?> StartUnix { get; private set; }

        internal MetadataVariableT<long?> EndUnix { get; private set; }

        internal InfectionSettings Config { get; set; }

        public bool OneMinuteLeft { get; private set; } = false;

        public static bool IsBodyLogEnabled
        {
            get
            {
                var rig = Player.RigManager;
                if (rig != null)
                {
                    bool disabled = false;
                    var PCDs = rig?.gameObject?.GetComponentsInChildren<PullCordDevice>();
                    PCDs?.ForEach(x =>
                    {
                        if (!x._bodyLogEnabled)
                            disabled = true;
                    });
                    return !disabled;
                }
                return true;
            }
        }

        private readonly Dictionary<PlayerID, PlayerActionType> LastPlayerActions = [];

        private int _surivorsLastCheckedMinutes = 0;

        private static bool appliedDeathmatchSpawns = false;

        private bool WasStarted = false;

        public static TeamMetadata GetTeamMetadata(Team team)
        {
            if (team == Instance.Infected)
            {
                return Instance.InfectedMetadata;
            }
            else if (team == Instance.Survivors)
            {
                return Instance.SurvivorsMetadata;
            }
            else if (team == Instance.InfectedChildren)
            {
                if (Instance.Config.SyncWithInfected.Value)
                    return Instance.InfectedMetadata;
                else
                    return Instance.InfectedChildrenMetadata;
            }
            return null;
        }

        public override GroupElementData CreateSettingsGroup()
            => GamemodeMenuManager.CreateSettingsGroup();

        public override void OnGamemodeRegistered()
        {
            WasStarted = false;
            Instance = this;
            Config = new InfectionSettings();
            InfectedChildren.DisplayName = "Infected Children";
            FusionOverrides.OnValidateNametag += OnValidateNameTag;

            MultiplayerHooking.OnPlayerAction += OnPlayerAction;
            MultiplayerHooking.OnPlayerJoined += OnPlayerJoin;
            MultiplayerHooking.OnPlayerLeft += OnPlayerLeave;

            MultiplayerHooking.OnDisconnected += Cleanup;

            TeamManager.Register(this);
            TeamManager.AddTeam(Infected);
            TeamManager.AddTeam(Survivors);
            TeamManager.AddTeam(InfectedChildren);
            TeamManager.OnAssignedToTeam += OnAssignedToTeam;

            InfectedMetadata = new TeamMetadata(Infected, Instance, new TeamSettings(Defaults.InfectedStats));
            SurvivorsMetadata = new TeamMetadata(Survivors, Instance, new TeamSettings(Defaults.SurvivorsStats));
            InfectedChildrenMetadata = new TeamMetadata(InfectedChildren, Instance, new TeamSettings(Defaults.InfectedChildrenStats));

            InfectedLooking = new MetadataBool(nameof(InfectedLooking), Metadata);

            CountdownValue = new MetadataInt(nameof(CountdownValue), Metadata);

            StartUnix = new MetadataVariableT<long?>(nameof(StartUnix), Metadata);

            EndUnix = new MetadataVariableT<long?>(nameof(EndUnix), Metadata);

            Metadata.OnMetadataChanged += OnMetadataChanged;

            EventManager.RegisterEvent<ulong>(EventType.PlayerInfected, PlayerInfected, true);
            EventManager.RegisterEvent<string>(EventType.RefreshStats, RefreshStats, true);
            EventManager.RegisterEvent<SwapAvatarData>(EventType.SwapAvatar, SwapAvatarEvent, true);

            EventManager.RegisterEvent(EventType.TeleportToHost, TeleportToHost, true);

            EventManager.RegisterEvent(EventType.OneMinuteLeft, OneMinuteLeftEvent, true);
            EventManager.RegisterGlobalNotification(EventType.InfectedVictory, "Infected Won", "Everyone has been infected!", 4f, true);
            EventManager.RegisterGlobalNotification(EventType.SurvivorsVictory, "Survivors Won", "There were people not infected in time!", 4f, true);

            BoneMenuManager.Setup();
            VisionManager.Setup();
        }

        private void OneMinuteLeftEvent()
        {
            ShowNotification("Avatar Infection", "One minute left!", 3.5f);
            OneMinuteLeft = true;
        }

        private static void SwapAvatarEvent(SwapAvatarData data)
        {
            if (data.Target != PlayerIDManager.LocalPlatformID)
                return;

            SwapAvatar(data.Barcode);
        }

        private new void OnMetadataChanged(string key, string value)
        {
            base.OnMetadataChanged(key, value);

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            if (!IsStarted)
                return;

            switch (key)
            {
                case nameof(InfectedLooking):
                    if (InfectedLooking.GetValue())
                        InfectedLookingEvent();
                    break;
            }
        }

        public override void OnGamemodeUnregistered()
        {
            FusionOverrides.OnValidateNametag -= OnValidateNameTag;

            MultiplayerHooking.OnPlayerAction -= OnPlayerAction;
            MultiplayerHooking.OnPlayerJoined -= OnPlayerJoin;
            MultiplayerHooking.OnPlayerLeft -= OnPlayerLeave;

            MultiplayerHooking.OnDisconnected -= Cleanup;

            TeamManager.Unregister();

            Metadata.OnMetadataChanged -= OnMetadataChanged;

            EventManager.OnUnregistered();

            BoneMenuManager.Destroy();
            VisionManager.Destroy();
        }

        private void OnPlayerLeave(PlayerID id)
        {
            if (!NetworkInfo.IsHost)
                return;

            if (!IsStarted)
                return;

            TeamManager.TryUnassignTeam(id);

            if (Infected.PlayerCount == 0 && InfectedChildren.PlayerCount == 0)
            {
                EventManager.TryInvokeEvent(EventType.SurvivorsVictory);
                GamemodeManager.StopGamemode();
            }
            else if (Survivors.PlayerCount == 0)
            {
                EventManager.TryInvokeEvent(EventType.InfectedVictory);
                GamemodeManager.StopGamemode();
            }
        }

        private void OnPlayerJoin(PlayerID playerId)
        {
            if (!NetworkInfo.IsHost)
                return;

            if (!IsStarted)
                return;

            if (TeamManager.GetPlayerTeam(playerId) == null)
                TeamManager.TryAssignTeam(playerId, Survivors);
        }

        private void RefreshStats(string teamName)
        {
            Team team = TeamManager.GetTeamByName(teamName);

            if (team != null && TeamManager.GetLocalTeam() == team)
                SetStats();
        }

        private void InfectedLookingEvent()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() != Infected && TeamManager.GetLocalTeam() != InfectedChildren)
                ShowNotification("Run...", "The infected have awaken... you have to run... save yourselves.. please", 5f, type: NotificationType.WARNING);
        }

        private void PlayerInfected(ulong userId)
        {
            if (!IsStarted)
                return;

            var playerId = PlayerIDManager.GetPlayerID(userId);

            if (playerId == null)
                return;

            if (NetworkInfo.IsHost && Survivors.HasPlayer(playerId))
            {
                if (Survivors.PlayerCount <= 1)
                {
                    EventManager.TryInvokeEvent(EventType.InfectedVictory);
                    GamemodeManager.StopGamemode();
                }
                else
                {
                    TeamManager.TryAssignTeam(playerId, InfectedChildren);
                    EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(playerId.PlatformID, Config.SelectedAvatar.ClientValue));
                }
            }

            if (playerId.IsMe && !HasBeenInfected)
            {
                ShowNotification("Infected", "Oh no, you got infected! Now you have to infect others...", 4f);

                HasBeenInfected = true;
            }
            else if (!playerId.IsMe)
            {
                playerId.TryGetDisplayName(out var displayName);

                ShowNotification(
                    "Infected",
                    $"{(string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName)} is now infected, {(Survivors.PlayerCount > 1 ? "look out for them..." : "the last survivor has fallen...")} ({Survivors.PlayerCount} survivors left)",
                    4f);
            }

            BoneMenuManager.PopulatePage();
        }

        internal static void SwapAvatar(string barcode, ModResult downloadResult = ModResult.SUCCEEDED)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode == Il2CppSLZ.Marrow.Warehouse.Barcode.EMPTY)
            {
                Logger.Error("ALERT! ALERT! This is not supposed to fucking happen, what the fuck did you do that the SelectedAvatar is empty. Now relax, calm down and fix this issue\nfuck you rottencheese, this shit will never work.");
                return;
            }

            if (Player.RigManager == null)
                return;

            bool hasCrate = CrateFilterer.HasCrate<AvatarCrate>(new(barcode));
            if (hasCrate)
            {
                var obj = new GameObject("AI_PCFC");
                var comp = obj.AddComponent<PullCordForceChange>();
                comp.avatarCrate = new AvatarCrateReference(barcode);
                comp.rigManager = Player.RigManager;
                comp.ForceChange(comp.rigManager.gameObject);
            }
            else
            {
                if (!ClientSettings.Downloading.DownloadAvatars.Value)
                    return;

                if (downloadResult == ModResult.FAILED)
                    return;

                NetworkModRequester.RequestAndInstallMod(new NetworkModRequester.ModInstallInfo()
                {
                    Barcode = barcode,
                    Target = PlayerIDManager.LocalSmallID,
                    FinishDownloadCallback = (ev) => SwapAvatar(barcode, ev.result),
                    MaxBytes = DataConversions.ConvertMegabytesToBytes(ClientSettings.Downloading.MaxFileSize.Value),
                    HighPriority = true
                });
            }
        }

        private IEnumerator InfectedLookingWait()
        {
            float remaining = Config.CountdownLength.ClientValue;

            while (remaining > 0)
            {
                remaining -= TimeUtilities.DeltaTime;
                var round = (int)remaining;
                if (CountdownValue.GetValue() != round)
                    CountdownValue.SetValue(round);
                yield return null;
            }

            InfectedLooking.SetValue(true);
        }

        private void OnAssignedToTeam(PlayerID player, Team team)
        {
            FusionOverrides.ForceUpdateOverrides();
            BoneMenuManager.PopulatePage();

            if (team == null || player?.IsValid != true)
                return;

            if (!player.IsMe)
                return;

            SetStats();
            var config = GetTeamMetadata(team);
            config?.CanUseGunsChanged();
            if (team != Survivors)
                SetBodyLog(false);
            else
                SetBodyLog(true);

            if (!InitialTeam)
                return;

            InitialTeam = false;

            if (team != Infected)
                ShowNotification("Survivor", "You got lucky! Make sure you don't get infected!", 3);

            if (!InfectedLooking.GetValue())
                VisionManager.HideVisionAndReveal(team != Infected ? 3 : 0);
        }

        public static void ShowNotification
            (string title,
             string message,
             float popupLength,
             bool showPopup = true,
             NotificationType type = NotificationType.INFORMATION,
             bool saveToMenu = false,
             Action onAccepted = null,
             Action onDeclined = null)
        {
            Notifier.Send(new Notification
            {
                Message = message,
                Title = title,
                PopupLength = popupLength,
                ShowPopup = showPopup,
                Type = type,
                OnAccepted = onAccepted,
                OnDeclined = onDeclined,
                SaveToMenu = saveToMenu
            });
        }

        protected override void OnUpdate()
        {
            _elapsedTime += TimeUtilities.DeltaTime;

            if (TeamManager.GetLocalTeam() == Survivors)
                SurvivorsUpdate();

            if (IsStarted)
                SetBodyLog(TeamManager.GetLocalTeam() == Survivors);
            else if (!IsBodyLogEnabled)
                SetBodyLog(true);

            if (!Config.NoTimeLimit.Value && NetworkInfo.IsHost)
            {
                if (!OneMinuteLeft && Config.TimeLimit.Value - ElapsedMinutes == 1)
                    EventManager.TryInvokeEvent(EventType.OneMinuteLeft);

                if (ElapsedMinutes >= Config.TimeLimit.Value)
                {
                    EventManager.TryInvokeEvent(EventType.SurvivorsVictory);
                    GamemodeManager.StopGamemode();
                }
            }
        }

        private void SurvivorsUpdate()
        {
            if (_surivorsLastCheckedMinutes != ElapsedMinutes)
            {
                _surivorsLastCheckedMinutes = ElapsedMinutes;

                PointItemManager.RewardBits(Defaults.SurvivorsBitReward);
            }
        }

        public override bool CheckReadyConditions()
        {
#if !DEBUG
            if (NetworkPlayer.Players.Count < 2)
                return false;

            if (string.IsNullOrWhiteSpace(SelectedAvatar))
                return false;

            var selected = new Barcode(SelectedAvatar);
            if (selected?.IsValid() != true || selected?.IsValidSize() != true)
                return false;

            if (InfectedCount > NetworkPlayer.Players.Count - 1)
                return false;
#endif
            return true;
        }

        private static void Internal_SetStats(TeamMetadata metadata)
        {
            // Push nametag updates
            FusionOverrides.ForceUpdateOverrides();

            float? jumpPower = metadata.JumpPower.ClientEnabled ? metadata.JumpPower.ClientValue : null;
            float? speed = metadata.Speed.ClientEnabled ? metadata.Speed.ClientValue : null;
            float? agility = metadata.Agility.ClientEnabled ? metadata.Agility.ClientValue : null;
            float? strengthUpper = metadata.StrengthUpper.ClientEnabled ? metadata.StrengthUpper.ClientValue : null;

            FusionPlayerExtended.SetOverrides(jumpPower, speed, agility, strengthUpper);

            // Force mortality
            LocalHealth.MortalityOverride = metadata.Mortality.ClientValue;

            if (metadata.Vitality.ClientEnabled) LocalHealth.VitalityOverride = metadata.Vitality.ClientValue;
        }

        internal void SetStats()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() == null)
                ClearOverrides();
            else
                Internal_SetStats(GetTeamMetadata(TeamManager.GetLocalTeam()));
        }

        private void ApplyGamemodeSettings()
        {
            if (Config.SelectMode.Value == AvatarSelectMode.RANDOM)
            {
                var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
                avatars.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));
                Config.SelectedAvatar.ClientValue = avatars.Random().Barcode.ID;
            }

            CountdownValue.SetValue(Config.CountdownLength.ClientValue);

            InfectedMetadata.ApplyConfig();
            InfectedChildrenMetadata.ApplyConfig();
            SurvivorsMetadata.ApplyConfig();

            var now = DateTimeOffset.Now;
            StartUnix.SetValue(now.ToUnixTimeMilliseconds());
            if (!Config.NoTimeLimit.Value)
                EndUnix.SetValue(now.AddMinutes(Config.TimeLimit.Value).ToUnixTimeMilliseconds());
            else
                EndUnix.SetValue(-1);

            InfectedLooking.SetValue(false);
        }

        public override void OnGamemodeStarted()
        {
            base.OnGamemodeStarted();

            PlayList.SetPlaylist(AudioReference.CreateReferences(Defaults.Tracks));
            PlayList.Shuffle();

            HasBeenInfected = false;
            _elapsedTime = 0f;
            _surivorsLastCheckedMinutes = 0;
            OneMinuteLeft = false;
            VisionManager.HideVision = false;
            InitialTeam = true;
            WasStarted = true;

            if (NetworkInfo.IsHost)
            {
                ApplyGamemodeSettings();
                AssignTeams();
                MelonCoroutines.Start(InfectedLookingWait());

                if (Config.TeleportOnStart.Value)
                    EventManager.TryInvokeEvent(EventType.TeleportToHost);
            }

            PlayList.StartPlaylist();

            // Invoke player changes on level load
            FusionSceneManager.HookOnTargetLevelLoad(() =>
            {
                BoneMenuManager.PopulatePage();
                SetStats();

                Config.SelectedPlayerOverride();

                if (Config.UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(!Config.TeleportOnStart.Value);
                else
                    ClearDeathmatchSpawns();
            });
        }

        internal static void UseDeathmatchSpawns_Init(bool teleport = true)
        {
            if (appliedDeathmatchSpawns)
                return;

            appliedDeathmatchSpawns = true;

            var transforms = new List<Transform>();

            foreach (var marker in GamemodeMarker.FilterMarkers(null))
            {
                transforms.Add(marker.transform);
            }

            FusionPlayer.SetSpawnPoints([.. transforms]);

            // Teleport to a random spawn point
            if (FusionPlayer.TryGetSpawnPoint(out var spawn) && teleport)
                LocalPlayer.TeleportToPosition(spawn.position, spawn.forward);
        }

        internal static void ClearDeathmatchSpawns()
        {
            appliedDeathmatchSpawns = false;
            FusionPlayer.ResetSpawnPoints();
        }

        private static void ClearOverrides()
        {
            // Reset mortality
            LocalHealth.MortalityOverride = null;

            LocalHealth.VitalityOverride = null;

            FusionPlayerExtended.ClearAllOverrides();

            FusionPlayerExtended.ClearAvatarOverride();
        }

        public override void OnGamemodeStopped()
        {
            base.OnGamemodeStopped();
            Cleanup();

            if (NetworkInfo.IsHost)
            {
                TeamManager.UnassignAllPlayers();
            }
            else
            {
                if (Config.TeleportOnEnd.ClientValue)
                    TeleportToHost();
            }
        }

        private void Cleanup()
        {
            if (WasStarted)
            {
                WasStarted = false;
                BoneMenuManager.PopulatePage();
                VisionManager.HideVision = false;

                HasBeenInfected = false;
                InitialTeam = true;
                _elapsedTime = 0f;
                _surivorsLastCheckedMinutes = 0;
                OneMinuteLeft = false;

                PlayList.StopPlaylist();

                SetBodyLog(true);

                ClearDeathmatchSpawns();

                ClearOverrides();

                FusionOverrides.ForceUpdateOverrides();
            }
        }

        public static void SetBodyLog(bool enabled)
        {
            if (IsBodyLogEnabled == enabled)
                return;
            var rig = Player.RigManager;
            if (rig != null)
            {
                var PCDs = rig?.gameObject?.GetComponentsInChildren<PullCordDevice>();
                PCDs?.ForEach(x => x._bodyLogEnabled = enabled);
            }
        }

        private static void TeleportToHost()
        {
            if (NetworkInfo.IsHost)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(PlayerIDManager.GetHostID(), out var player))
                return;

            if (player.HasRig)
            {
                var feetPosition = player.RigRefs.RigManager.physicsRig.feet.transform.position;

                LocalPlayer.TeleportToPosition(feetPosition, Vector3.forward);
            }
        }

        protected bool OnValidateNameTag(PlayerID id)
        {
            if (!IsStarted)
                return true;

            var playerTeam = TeamManager.GetPlayerTeam(id);
            var localTeam = TeamManager.GetLocalTeam();

            return playerTeam == localTeam ||
                (playerTeam == Infected && localTeam == InfectedChildren) ||
                (playerTeam == InfectedChildren && localTeam == Infected);
        }

        private void AssignTeams()
        {
            var players = new List<PlayerID>(PlayerIDManager.PlayerIDs);
            players.Shuffle();

            bool selected = false;
            for (int i = 0; i < Config.InfectedCount.Value; i++)
            {
                var player = players.Random();
                TeamManager.TryAssignTeam(player, Infected);
                EventManager.TryInvokeEvent(EventType.SwapAvatar,
                    new SwapAvatarData(player.PlatformID, Config.SelectedAvatar.ClientValue));

                if (Config.SelectMode.Value == AvatarSelectMode.FIRST_INFECTED && !selected
                    && NetworkPlayerManager.TryGetPlayer(player.SmallID, out NetworkPlayer plr) && plr.HasRig)
                {
                    var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                    if (!string.IsNullOrWhiteSpace(avatar))
                    {
                        selected = true;
                        Config.SelectedAvatar.ClientValue = avatar;
                    }
                }

                players.Remove(player);
            }

            foreach (var plr in players)
                TeamManager.TryAssignTeam(plr, Survivors);
        }

        protected void OnPlayerAction(PlayerID player, PlayerActionType type, PlayerID otherPlayer = null)
        {
            if (!IsStarted)
                return;

            if (type == PlayerActionType.DYING_BY_OTHER_PLAYER)
            {
                if (!NetworkInfo.IsHost || otherPlayer == null || Config.InfectType.Value != InfectType.DEATH)
                    return;

                var playerTeam = TeamManager.GetPlayerTeam(player);
                var otherPlayerTeam = TeamManager.GetPlayerTeam(otherPlayer);

                if (playerTeam == Survivors &&
                    (otherPlayerTeam == Infected || otherPlayerTeam == InfectedChildren))
                {
                    EventManager.TryInvokeEvent(EventType.PlayerInfected, player.PlatformID);
                }
            }
            else if (type == PlayerActionType.DYING)
            {
                if (!NetworkInfo.IsHost || !Config.SuicideInfects.Value || otherPlayer != null)
                    return;

                if (LastPlayerActions.ContainsKey(player) && LastPlayerActions[player] == PlayerActionType.DYING_BY_OTHER_PLAYER)
                    return;

                if (TeamManager.GetPlayerTeam(player) == Survivors)
                    EventManager.TryInvokeEvent(EventType.PlayerInfected, player.PlatformID);
            }
            LastPlayerActions[player] = type;
        }

        public enum InfectType
        {
            TOUCH = 0,
            DEATH = 1
        }

        public enum AvatarSelectMode
        {
            CONFIG,

            FIRST_INFECTED,

            RANDOM
        }

        public enum EventType
        {
            PlayerInfected,
            SwapAvatar,
            RefreshStats,
            TeleportToHost,

            OneMinuteLeft,
            InfectedVictory,
            SurvivorsVictory
        }
    }

    internal class SwapAvatarData(ulong target, string barcode)
    {
        public ulong Target { get; set; } = target;

        public string Barcode { get; set; } = barcode;
    }
}