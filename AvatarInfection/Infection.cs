// Ignore Spelling: Metadata Unragdoll

using System;
using System.Collections.Generic;
using System.Collections;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Menu.Data;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Extensions;
using LabFusion.SDK.Points;
using LabFusion.Marrow;
using LabFusion.Scene;
using LabFusion.Senders;
using LabFusion.Marrow.Integration;
using LabFusion.SDK.Metadata;
using LabFusion.UI.Popups;

using UnityEngine;

using MelonLoader;

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

        public override string Barcode => Constants.Defaults.Barcode;

        public override string Description => "An infection is spreading, turning people into a selected avatar by the host.";

        public override Texture Logo => Core.Icon;

        public override bool DisableSpawnGun => Config?.DisableSpawnGun?.ClientValue ?? true;


        public override bool DisableDevTools => Config?.DisableDevTools?.ClientValue ?? true;


        public override bool AutoHolsterOnDeath => true;

        public override bool DisableManualUnragdoll => true;

        public override bool AutoStopOnSceneLoad => true;

        public override bool ManualReady => false;

        public static Infection Instance { get; private set; }

        internal InfectionTeam Infected { get; private set; }

        internal InfectionTeam Survivors { get; private set; }

        internal InfectionTeam InfectedChildren { get; private set; }

        public bool IsInfected(InfectionTeam team) => team != null && (team.Team == Infected.Team || team.Team == InfectedChildren.Team);

        public bool IsPlayerInfected(PlayerID id) => IsInfected(TeamManager?.GetPlayerTeam(id));

        public bool IsLocalPlayerInfected() => IsInfected(TeamManager?.GetLocalTeam());

        internal InfectionTeamManager TeamManager { get; } = new();

        public MusicPlaylist PlayList { get; } = new();

        internal MetadataBool InfectedLooking { get; private set; }

        internal bool InitialTeam { get; private set; } = true;

        public bool HasBeenInfected { get; private set; } = false;

        private float _elapsedTime;

        public float ElapsedSeconds => _elapsedTime;
        public int ElapsedMinutes => Mathf.FloorToInt(ElapsedSeconds / 60f);

        internal MetadataInt CountdownValue { get; private set; }

        internal MetadataVariableT<long?> StartUnix { get; private set; }

        internal MetadataVariableT<long?> EndUnix { get; private set; }

        internal InfectionSettings Config { get; set; }

        public bool OneMinuteLeft { get; private set; } = false;

        private readonly Dictionary<PlayerID, PlayerActionType> LastPlayerActions = [];

        private int _surivorsLastCheckedMinutes;

        private static bool appliedDeathmatchSpawns;

        private bool WasStarted = false;

        public override GroupElementData CreateSettingsGroup()
            => GamemodeMenuManager.CreateSettingsGroup();

        public override void OnGamemodeRegistered()
        {
            WasStarted = false;
            Instance = this;
            Config = new InfectionSettings();

            FusionOverrides.OnValidateNametag += OnValidateNameTag;

            MultiplayerHooking.OnPlayerAction += OnPlayerAction;
            MultiplayerHooking.OnPlayerJoined += OnPlayerJoin;
            MultiplayerHooking.OnPlayerLeft += OnPlayerLeave;

            MultiplayerHooking.OnDisconnected += Cleanup;

            Infected = new(new("Infected"), Color.green, this, new(Constants.Defaults.InfectedStats));
            Survivors = new(new("Survivors"), Color.cyan, this, new(Constants.Defaults.SurvivorsStats));
            InfectedChildren = new(new("InfectedChildren"), new Color(0, 1, 0), this, new(Constants.Defaults.InfectedChildrenStats));
            InfectedChildren.Team.DisplayName = "Infected Children";

            TeamManager.Register(this);
            TeamManager.AddTeam(Infected);
            TeamManager.AddTeam(Survivors);
            TeamManager.AddTeam(InfectedChildren);
            TeamManager.OnAssignedToInfectedTeam += OnAssignedToTeam;

            InfectedLooking = new MetadataBool(nameof(InfectedLooking), Metadata);

            CountdownValue = new MetadataInt(nameof(CountdownValue), Metadata);

            StartUnix = new MetadataVariableT<long?>(nameof(StartUnix), Metadata);

            EndUnix = new MetadataVariableT<long?>(nameof(EndUnix), Metadata);

            Metadata.OnMetadataChanged += OnMetadataChanged;

            EventManager.RegisterEvent<string>(EventType.RefreshStats, StatsManager.RefreshStats, true);
            EventManager.RegisterEvent<ulong>(EventType.PlayerInfected, PlayerInfected, true);
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
            MenuHelper.ShowNotification("Avatar Infection", "One minute left!", 3.5f);
            OneMinuteLeft = true;
        }

        private static void SwapAvatarEvent(SwapAvatarData data)
        {
            if (data.Target != PlayerIDManager.LocalPlatformID)
                return;

            FusionPlayerExtended.SetAvatarOverride(data.Barcode);
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

            if (Infected.Team.PlayerCount == 0 && InfectedChildren.Team.PlayerCount == 0)
            {
                EventManager.TryInvokeEvent(EventType.SurvivorsVictory);
                GamemodeManager.StopGamemode();
            }
            else if (Survivors.Team.PlayerCount == 0)
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
                TeamManager.TryAssignTeam(playerId, Survivors.Team);
        }

        private void InfectedLookingEvent()
        {
            if (!IsStarted)
                return;

            if (!IsInfected(TeamManager.GetLocalTeam()))
                MenuHelper.ShowNotification("Run...", "The infected have awaken... you have to run... save yourselves.. please", 5f, type: NotificationType.WARNING);
        }

        private void PlayerInfected(ulong userId)
        {
            if (!IsStarted)
                return;

            var playerId = PlayerIDManager.GetPlayerID(userId);

            if (playerId == null)
                return;

            if (NetworkInfo.IsHost && Survivors.Team.HasPlayer(playerId))
            {
                if (Survivors.Team.PlayerCount <= 1)
                {
                    EventManager.TryInvokeEvent(EventType.InfectedVictory);
                    GamemodeManager.StopGamemode();
                }
                else
                {
                    TeamManager.TryAssignTeam(playerId, InfectedChildren.Team);
                    EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(playerId.PlatformID, Config.SelectedAvatar.ClientValue));
                }
            }

            if (playerId.IsMe && !HasBeenInfected)
            {
                MenuHelper.ShowNotification("Infected", "Oh no, you got infected! Now you have to infect others...", 4f);

                HasBeenInfected = true;
            }
            else if (!playerId.IsMe)
            {
                playerId.TryGetDisplayName(out var displayName);
                string _displayName = string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName;
                string last = "look out for them...";

                if (Survivors.Team.PlayerCount > 1)
                    last = "the last survivor has fallen...";

                MenuHelper.ShowNotification(
                    "Infected",
                    $"{_displayName} is now infected, {last} ({Survivors.Team.PlayerCount} survivors left)",
                    4f);
            }

            BoneMenuManager.PopulatePage();
        }

        private IEnumerator InfectedLookingWait()
        {
            int remaining = Config.CountdownLength.ClientValue;

            while (remaining > 0)
            {
                yield return new WaitForSeconds(1);
                remaining--;
                if (CountdownValue.GetValue() != remaining)
                    CountdownValue.SetValue(remaining);
            }

            InfectedLooking.SetValue(true);
        }

        private void OnAssignedToTeam(PlayerID player, InfectionTeam team)
        {
            FusionOverrides.ForceUpdateOverrides();
            BoneMenuManager.PopulatePage();

            if (team == null || player?.IsValid != true)
                return;

            if (!player.IsMe)
                return;

            StatsManager.ApplyStats();

            team?.Metadata.CanUseGunsChanged();

            if (!InitialTeam)
                return;

            InitialTeam = false;

            if (team != Infected)
                MenuHelper.ShowNotification("Survivor", "You got lucky! Make sure you don't get infected!", 3);

            if (!InfectedLooking.GetValue())
                VisionManager.HideVisionAndReveal(team != Infected ? 3 : 0);
        }

        protected override void OnUpdate()
        {
            _elapsedTime += TimeUtilities.DeltaTime;

            if (TeamManager.GetLocalTeam() == Survivors)
                SurvivorsUpdate();

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

                PointItemManager.RewardBits(Constants.Defaults.SurvivorsBitReward);
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

        private void ApplyGamemodeSettings()
        {
            if (Config.SelectMode.Value == AvatarSelectMode.RANDOM)
            {
                var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
                avatars.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));
                Config.SelectedAvatar.ClientValue = avatars.Random().Barcode.ID;
            }

            CountdownValue.SetValue(Config.CountdownLength.ClientValue);

            Infected.Metadata.ApplyConfig();
            InfectedChildren.Metadata.ApplyConfig();
            Survivors.Metadata.ApplyConfig();

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

            PlayList.SetPlaylist(AudioReference.CreateReferences(Constants.Defaults.Tracks));
            PlayList.Shuffle();

            RevertToDefault();

            if (NetworkInfo.IsHost)
            {
                ApplyGamemodeSettings();
                AssignTeams();
                MelonCoroutines.Start(InfectedLookingWait());

                if (Config.TeleportOnStart.Value)
                    EventManager.TryInvokeEvent(EventType.TeleportToHost);
            }

            PlayList.StartPlaylist();

            FusionSceneManager.HookOnTargetLevelLoad(() =>
            {
                BoneMenuManager.PopulatePage();
                StatsManager.ApplyStats();

                Config.SelectedPlayerOverride();

                if (Config.UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(!Config.TeleportOnStart.Value);
                else
                    ClearDeathmatchSpawns();
            });
        }

        private void RevertToDefault(bool wasStarted = true)
        {
            HasBeenInfected = false;
            _elapsedTime = 0f;
            _surivorsLastCheckedMinutes = 0;
            OneMinuteLeft = false;
            VisionManager.HideVision = false;
            InitialTeam = true;
            WasStarted = wasStarted;
        }

        internal static void UseDeathmatchSpawns_Init(bool teleport = true)
        {
            if (appliedDeathmatchSpawns)
                return;

            appliedDeathmatchSpawns = true;

            List<Transform> transforms = [];
            GamemodeMarker.FilterMarkers(null).ForEach(x => transforms.Add(x.transform));

            FusionPlayer.SetSpawnPoints([.. transforms]);

            if (FusionPlayer.TryGetSpawnPoint(out var spawn) && teleport)
                LocalPlayer.TeleportToPosition(spawn.position, spawn.forward);
        }

        internal static void ClearDeathmatchSpawns()
        {
            appliedDeathmatchSpawns = false;
            FusionPlayer.ResetSpawnPoints();
        }

        public override void OnGamemodeStopped()
        {
            base.OnGamemodeStopped();
            Cleanup();

            if (!NetworkInfo.HasServer)
                return;

            if (NetworkInfo.IsHost)
                TeamManager.UnassignAllPlayers();
            else if (Config.TeleportOnEnd.ClientValue)
                TeleportToHost();
        }

        private void Cleanup()
        {
            if (WasStarted)
            {
                BoneMenuManager.PopulatePage();

                RevertToDefault(false);

                PlayList.StopPlaylist();

                ClearDeathmatchSpawns();

                StatsManager.ClearOverrides();

                FusionOverrides.ForceUpdateOverrides();
            }
        }

        private static void TeleportToHost()
        {
            if (NetworkInfo.IsHost)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(PlayerIDManager.GetHostID(), out var player))
                return;

            if (player.HasRig)
                LocalPlayer.TeleportToPosition(player.RigRefs.RigManager.physicsRig.feet.transform.position, Vector3.forward);
        }

        protected bool OnValidateNameTag(PlayerID id)
        {
            if (!IsStarted)
                return true;

            var playerTeam = TeamManager.GetPlayerTeam(id);
            var localTeam = TeamManager.GetLocalTeam();

            return playerTeam == localTeam || (IsInfected(playerTeam) && IsInfected(localTeam));
        }

        private void AssignTeams()
        {
            var players = new List<PlayerID>(PlayerIDManager.PlayerIDs);
            players.Shuffle();

            bool selected = false;
            for (int i = 0; i < Config.InfectedCount.Value; i++)
            {
                var player = players.Random();
                TeamManager.TryAssignTeam(player, Infected.Team);
                bool exists = NetworkPlayerManager.TryGetPlayer(player.SmallID, out NetworkPlayer plr) && plr.HasRig;

                if (Config.SelectMode.Value == AvatarSelectMode.FIRST_INFECTED && !selected && exists)
                {
                    var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                    if (!string.IsNullOrWhiteSpace(avatar))
                    {
                        selected = true;
                        Config.SelectedAvatar.ClientValue = avatar;
                    }
                }

                EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(player.PlatformID, Config.SelectedAvatar.ClientValue));

                players.Remove(player);
            }

            players.ForEach(x => TeamManager.TryAssignTeam(x, Survivors.Team));
        }

        protected void OnPlayerAction(PlayerID player, PlayerActionType type, PlayerID otherPlayer = null)
        {
            if (!IsStarted || !NetworkInfo.IsHost)
                return;

            if (type == PlayerActionType.DYING_BY_OTHER_PLAYER)
                KilledEvent(player, otherPlayer);
            else if (type == PlayerActionType.DYING)
                DyingEvent(player, otherPlayer);

            LastPlayerActions[player] = type;
        }

        private void DyingEvent(PlayerID player, PlayerID otherPlayer)
        {
            if (!Config.SuicideInfects.Value || otherPlayer != null)
                return;

            if (LastPlayerActions.ContainsKey(player) && LastPlayerActions[player] == PlayerActionType.DYING_BY_OTHER_PLAYER)
                return;

            if (TeamManager.GetPlayerTeam(player) == Survivors)
                EventManager.TryInvokeEvent(EventType.PlayerInfected, player.PlatformID);
        }

        private void KilledEvent(PlayerID player, PlayerID killer)
        {
            if (killer == null || Config.InfectType.Value != InfectType.DEATH)
                return;

            if (TeamManager.GetPlayerTeam(player) == Survivors && IsPlayerInfected(killer))
                EventManager.TryInvokeEvent(EventType.PlayerInfected, player.PlatformID);
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