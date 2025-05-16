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

using UnityEngine;

using MelonLoader;

using BoneLib;

using AvatarInfection.Utilities;
using AvatarInfection.Helper;

using LabFusion.RPC;
using LabFusion.Preferences.Client;
using LabFusion.Downloading;
using LabFusion.Data;
using AvatarInfection.Managers;
using AvatarInfection.Settings;

namespace AvatarInfection
{
    public class Infection : Gamemode
    {
        public override string Title => "Avatar Infection";

        public override string Author => "HAHOOS";

        public override string Barcode => Defaults.Barcode;

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

            public const bool DontShowAnyNametags = false;

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
            BONELABMonoDiscReferences.TheRecurringDreamReference,
            BONELABMonoDiscReferences.HeavyStepsReference,
            BONELABMonoDiscReferences.StankFaceReference,
            BONELABMonoDiscReferences.AlexInWonderlandReference,
            BONELABMonoDiscReferences.ItDoBeGroovinReference,

            BONELABMonoDiscReferences.ConcreteCryptReference, // concrete crypt
        ];
        }

        public MusicPlaylist PlayList { get; } = new();

        internal MetadataBool InfectedLooking { get; private set; }

        internal bool InitialTeam { get; private set; } = true;

        internal bool HasBeenInfected { get; private set; } = false;

        private static ModuleLogger Logger => FusionModule.Logger;

        private float _elapsedTime = 0f;

        internal float ElapsedSeconds => _elapsedTime;
        internal int ElapsedMinutes => Mathf.FloorToInt(ElapsedSeconds / 60f);

        internal MetadataInt CountdownValue { get; private set; }

        internal MetadataVariableT<long?> StartUnix { get; private set; }

        internal MetadataVariableT<long?> EndUnix { get; private set; }

        internal InfectionSettings Config { get; set; }

        private readonly Dictionary<PlayerId, PlayerActionType> LastPlayerActions = [];

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
            Instance = this;
            Config = new InfectionSettings();
            InfectedChildren.DisplayName = "Infected Children";
            FusionOverrides.OnValidateNametag += OnValidateNameTag;

            MultiplayerHooking.OnPlayerAction += OnPlayerAction;
            MultiplayerHooking.OnPlayerJoin += OnPlayerJoin;
            MultiplayerHooking.OnPlayerLeave += OnPlayerLeave;

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

            EventManager.RegisterGlobalNotification(EventType.OneMinuteLeft, "Avatar Infection", "One minute left!", 3.5f, true);
            EventManager.RegisterGlobalNotification(EventType.InfectedVictory, "Infected Won", "Everyone has been infected!", 4f, true);
            EventManager.RegisterGlobalNotification(EventType.SurvivorsVictory, "Survivors Won", "There were people not infected in time!", 4f, true);

            BoneMenuManager.Setup();
            VisionManager.Setup();
        }

        private static void SwapAvatarEvent(SwapAvatarData data)
        {
            if (data.Target != PlayerIdManager.LocalLongId)
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
            MultiplayerHooking.OnPlayerJoin -= OnPlayerJoin;
            MultiplayerHooking.OnPlayerLeave -= OnPlayerLeave;

            TeamManager.Unregister();

            Metadata.OnMetadataChanged -= OnMetadataChanged;

            EventManager.OnUnregistered();

            BoneMenuManager.Destroy();
            VisionManager.Destroy();
        }

        private void OnPlayerLeave(PlayerId id)
        {
            if (!NetworkInfo.IsServer)
                return;

            if (!IsStarted)
                return;

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

        private void OnPlayerJoin(PlayerId playerId)
        {
            if (!NetworkInfo.IsServer)
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

            var playerId = PlayerIdManager.GetPlayerId(userId);

            if (playerId == null)
                return;

            if (NetworkInfo.IsServer && Survivors.HasPlayer(playerId))
            {
                if (Survivors.PlayerCount <= 1)
                {
                    EventManager.TryInvokeEvent(EventType.InfectedVictory);
                    GamemodeManager.StopGamemode();
                }
                else
                {
                    TeamManager.TryAssignTeam(playerId, InfectedChildren);
                    EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(playerId.LongId, Config.SelectedAvatar.ClientValue));
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
                    $"{(string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName)} is now infected, {(Survivors.PlayerCount > 1 ? "look out for them..." : "the last survivor has fallen...")}",
                    4f);
            }

            BoneMenuManager.PopulatePage();
        }

        internal static void SwapAvatar(string barcode, ModResult downloadResult = ModResult.SUCCEEDED)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode == Il2CppSLZ.Marrow.Warehouse.Barcode.EMPTY)
            {
                Logger.Error("ALERT! ALERT! This is not supposed to fucking happen, what the fuck did you do that the SelectedAvatar is empty. Now relax, calm down and fix this issue");
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
                PullCordSender.SendBodyLogEffect();
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
                    barcode = barcode,
                    target = PlayerIdManager.LocalSmallId,
                    finishDownloadCallback = (ev) => SwapAvatar(barcode, ev.result),
                    maxBytes = DataConversions.ConvertMegabytesToBytes(ClientSettings.Downloading.MaxFileSize.Value),
                    highPriority = true
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

        private void OnAssignedToTeam(PlayerId player, Team team)
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
                ShowNotification("Survivor", "Woah! You got lucky. Make sure you don't get infected!", 3);

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
            FusionNotifier.Send(new FusionNotification
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

        internal bool _oneMinuteLeft = false;

        protected override void OnUpdate()
        {
            _elapsedTime += TimeUtilities.DeltaTime;

            if (TeamManager.GetLocalTeam() == Survivors)
                SurvivorsUpdate();

            if (IsStarted)
                SetBodyLog(TeamManager.GetLocalTeam() == Survivors);
            else if (!IsBodyLogEnabled)
                SetBodyLog(true);

            if (!Config.NoTimeLimit.Value)
            {
                if (!_oneMinuteLeft && Config.TimeLimit.Value - ElapsedMinutes == 1)
                {
                    if (NetworkInfo.IsServer) EventManager.TryInvokeEvent(EventType.OneMinuteLeft);
                    _oneMinuteLeft = true;
                }
                if (NetworkInfo.IsServer && ElapsedMinutes >= Config.TimeLimit.Value)
                {
                    EventManager.TryInvokeEvent(EventType.SurvivorsVictory);
                    GamemodeManager.StopGamemode();
                }
            }
        }

        private int _lastCheckedMinutes = 0;

        private void SurvivorsUpdate()
        {
            if (_lastCheckedMinutes != ElapsedMinutes)
            {
                _lastCheckedMinutes = ElapsedMinutes;

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
            FusionPlayer.SetMortality(metadata.Mortality.ClientValue);

            if (metadata.Vitality.ClientEnabled) FusionPlayer.SetPlayerVitality(metadata.Vitality.ClientValue);
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
                Config.SelectedAvatar.ClientValue = avatars[UnityEngine.Random.RandomRangeInt(0, avatars.Count)].Barcode.ID;
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
            _lastCheckedMinutes = 0;
            _oneMinuteLeft = false;
            VisionManager.HideVision = false;
            InitialTeam = true;

            if (NetworkInfo.IsServer)
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

        static bool appliedDeathmatchSpawns = false;

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
                FusionPlayer.Teleport(spawn.position, spawn.forward);
        }

        internal static void ClearDeathmatchSpawns()
        {
            appliedDeathmatchSpawns = false;
            FusionPlayer.ResetSpawnPoints();
        }

        private static void ClearOverrides()
        {
            // Reset mortality
            FusionPlayer.ResetMortality();

            FusionPlayer.ClearPlayerVitality();

            FusionPlayerExtended.ClearAllOverrides();

            FusionPlayerExtended.ClearAvatarOverride();
        }

        public override void OnGamemodeStopped()
        {
            base.OnGamemodeStopped();
            BoneMenuManager.PopulatePage();
            VisionManager.HideVision = false;

            HasBeenInfected = false;
            InitialTeam = true;

            PlayList.StopPlaylist();

            if (NetworkInfo.IsServer)
            {
                TeamManager.UnassignAllPlayers();
            }
            else
            {
                if (Config.TeleportOnEnd.ClientValue)
                    TeleportToHost();
            }

            SetBodyLog(true);

            _elapsedTime = 0f;
            _lastCheckedMinutes = 0;
            _oneMinuteLeft = false;

            ClearDeathmatchSpawns();

            ClearOverrides();

            FusionOverrides.ForceUpdateOverrides();
        }

        private static void SetBodyLog(bool enabled)
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

        private static bool IsBodyLogEnabled
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

        private static void TeleportToHost()
        {
            if (NetworkInfo.IsServer)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(PlayerIdManager.GetHostId(), out var player))
                return;

            if (player.HasRig)
            {
                var feetPosition = player.RigRefs.RigManager.physicsRig.feet.transform.position;

                FusionPlayer.Teleport(feetPosition, Vector3.forward, true);
            }
        }

        protected bool OnValidateNameTag(PlayerId id)
        {
            if (!IsStarted)
                return true;

            var playerTeam = TeamManager.GetPlayerTeam(id);
            var localTeam = TeamManager.GetLocalTeam();

            if (Config.DontShowAnyNametags.ClientValue)
                return false;

            return playerTeam == localTeam ||
                (playerTeam == Infected && localTeam == InfectedChildren) ||
                (playerTeam == InfectedChildren && localTeam == Infected);
        }

        private void AssignTeams()
        {
            var players = new List<PlayerId>(PlayerIdManager.PlayerIds);
            players.Shuffle();

            string selected = null;

            var rand = new System.Random();
            for (int i = 0; i < Config.InfectedCount.Value; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                TeamManager.TryAssignTeam(player, Infected);
                EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(player.LongId, Config.SelectedAvatar.ClientValue));

                if (Config.SelectMode.Value == AvatarSelectMode.FIRSTINFECTED && string.IsNullOrWhiteSpace(selected)
                    && NetworkPlayerManager.TryGetPlayer(player.SmallId, out NetworkPlayer plr) && plr.HasRig)
                {
                    var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                    if (!string.IsNullOrWhiteSpace(avatar))
                    {
                        selected = avatar;
                        Config.SelectedAvatar.ClientValue = avatar;
                    }
                }

                players.Remove(player);
            }

            foreach (var plr in players)
                TeamManager.TryAssignTeam(plr, Survivors);
        }

        protected void OnPlayerAction(PlayerId player, PlayerActionType type, PlayerId otherPlayer = null)
        {
            if (!IsStarted)
                return;

            if (type == PlayerActionType.DYING_BY_OTHER_PLAYER)
            {
                if (!NetworkInfo.IsServer || otherPlayer == null || Config.InfectType.Value != InfectType.DEATH)
                    return;

                var playerTeam = TeamManager.GetPlayerTeam(player);
                var otherPlayerTeam = TeamManager.GetPlayerTeam(otherPlayer);

                if (playerTeam == Survivors &&
                    (otherPlayerTeam == Infected || otherPlayerTeam == InfectedChildren))
                {
                    EventManager.TryInvokeEvent(EventType.PlayerInfected, player.LongId);
                }
            }
            else if (type == PlayerActionType.DYING)
            {
                if (!NetworkInfo.IsServer || !Config.SuicideInfects.Value || otherPlayer != null)
                    return;

                if (LastPlayerActions.ContainsKey(player) && LastPlayerActions[player] == PlayerActionType.DYING_BY_OTHER_PLAYER)
                    return;

                if (TeamManager.GetPlayerTeam(player) == Survivors)
                    EventManager.TryInvokeEvent(EventType.PlayerInfected, player.LongId);
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

            [EnumName("FIRST INFECTED")]
            FIRSTINFECTED,

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