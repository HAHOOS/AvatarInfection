// Ignore Spelling: Metadata Unragdoll

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Managers;
using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Entities;
using LabFusion.Extensions;
using LabFusion.Marrow;
using LabFusion.Marrow.Integration;
using LabFusion.Menu.Data;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Scene;
using LabFusion.SDK.Gamemodes;
using LabFusion.SDK.Metadata;
using LabFusion.SDK.Points;
using LabFusion.Senders;
using LabFusion.UI.Popups;
using LabFusion.Utilities;

using MelonLoader;

using UnityEngine;

namespace AvatarInfection
{
    public class Infection : Gamemode
    {
        public override string Title => Constants.Name;

        public override string Author => Constants.Author;

        public override string Barcode => Constants.Barcode;

        public override string Description => Constants.Description;

        public override Texture Logo => Core.Icon;

        public override bool DisableSpawnGun => Config?.DisableSpawnGun?.Value ?? true;

        public override bool DisableDevTools => Config?.DisableDevTools?.Value ?? true;

        public override bool AutoHolsterOnDeath => true;

        public override bool DisableManualUnragdoll => true;

        public override bool AutoStopOnSceneLoad => true;

        public override bool ManualReady => false;

        public static Infection Instance { get; private set; }

        internal InfectionTeam Infected { get; private set; }

        internal InfectionTeam Survivors { get; private set; }

        internal InfectionTeam InfectedChildren { get; private set; }

        public bool IsInfected(InfectionTeam team) => team != null && (team == Infected || team == InfectedChildren);

        public bool IsPlayerInfected(PlayerID id) => IsInfected(TeamManager?.GetPlayerTeam(id));

        public bool IsLocalPlayerInfected() => IsInfected(TeamManager?.GetLocalTeam());

        internal InfectionTeamManager TeamManager { get; } = new();

        public MusicPlaylist PlayList { get; } = new();

        internal MetadataBool InfectedLooking { get; private set; }

        internal bool InitialTeam { get; private set; } = true;

        public bool HasBeenInfected { get; private set; }

        private float _elapsedTime;

        public float ElapsedSeconds => _elapsedTime;
        public int ElapsedMinutes => Mathf.FloorToInt(ElapsedSeconds / 60f);

        internal MetadataInt CountdownValue { get; private set; }

        internal MetadataVariableT<long?> StartUnix { get; private set; }

        internal MetadataVariableT<long?> EndUnix { get; private set; }

        internal InfectionSettings Config { get; set; }

        public bool OneMinuteLeft { get; private set; }

        private readonly Dictionary<PlayerID, PlayerActionType> LastPlayerActions = [];

        private int _surivorsLastCheckedMinutes;

        private static bool appliedDeathmatchSpawns;

        private bool WasStarted;

        public const string HAS_AVATAR_INFECTED_KEY = "DoYouHaveAvatarInfection";

        private List<ulong> LastInfected { get; } = [];

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
            MultiplayerHooking.OnStartedServer += IHaveAvatarInfection;
            MultiplayerHooking.OnJoinedServer += IHaveAvatarInfection;
            MultiplayerHooking.OnDisconnected += Cleanup;

            Infected = new("Infected", Color.green, this, new(Constants.Defaults.InfectedStats));
            Survivors = new("Survivors", Color.cyan, this, new(Constants.Defaults.SurvivorsStats));
            InfectedChildren = new("InfectedChildren", new Color(0, 1, 0), this, new(Constants.Defaults.InfectedChildrenStats), GetInfectedChildrenMetadata)
            {
                DisplayName = "Infected Children"
            };

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
            EventManager.RegisterEvent<PlayerInfectedData>(EventType.PlayerInfected, PlayerInfected, true);
            EventManager.RegisterEvent<SwapAvatarData>(EventType.SwapAvatar, SwapAvatarEvent, true);

            EventManager.RegisterEvent(EventType.TeleportToHost, TeleportToHost, true);
            EventManager.RegisterEvent(EventType.OneMinuteLeft, OneMinuteLeftEvent, true);

            EventManager.RegisterNotification(EventType.InfectedVictory, "Infected Won", "Everyone has been infected!");
            EventManager.RegisterNotification(EventType.SurvivorsVictory, "Survivors Won", "There were people not infected in time!");

            BoneMenuManager.Setup();
            VisionManager.Setup();
        }

        private TeamMetadata GetInfectedChildrenMetadata()
        {
            if (!Config.SyncWithInfected.Value)
                return InfectedChildren.StaticMetadata;
            else
                return Infected.Metadata;
        }

        private static void IHaveAvatarInfection()
            => LocalPlayer.Metadata.Metadata.TrySetMetadata(HAS_AVATAR_INFECTED_KEY, bool.TrueString);

        private static bool DoYouHaveAvatarInfection(PlayerID player)
        => player.Metadata.Metadata.TryGetMetadata(HAS_AVATAR_INFECTED_KEY, out string val)
            && !string.IsNullOrWhiteSpace(val) && bool.TryParse(val, out bool res) && res;

#if !DEBUG && !SOLOTESTING

        private static int CountPlayersThatHaveAvatarInfection()
            => PlayerIDManager.PlayerIDs.Count(x => x.Metadata.Metadata.TryGetMetadata(HAS_AVATAR_INFECTED_KEY, out string val)
                && !string.IsNullOrWhiteSpace(val) && bool.TryParse(val, out bool res) && res);

#endif

        private void OneMinuteLeftEvent()
        {
            MenuHelper.ShowNotification("Avatar Infection", "One minute left!", 3.5f);
            OneMinuteLeft = true;
        }

        private static void SwapAvatarEvent(SwapAvatarData data)
        {
            if (data.Target != PlayerIDManager.LocalPlatformID)
                return;

            Overrides.SetAvatarOverride(data.Barcode, data.Origin);
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

        private void InfectedLookingEvent()
        {
            if (!IsStarted)
                return;

            if (!IsInfected(TeamManager.GetLocalTeam()))
                MenuHelper.ShowNotification("Run...", "The infected have awaken... you have to run... save yourselves.. please", 5f, type: NotificationType.WARNING);
        }

        private void PlayerInfected(PlayerInfectedData data)
        {
            var userId = data.UserId;
            var by = data.By;

            if (!IsStarted)
                return;

            var playerId = PlayerIDManager.GetPlayerID(userId);

            if (playerId == null)
                return;

            if (by != -1 && by == (long)PlayerIDManager.LocalPlatformID)
                PointItemManager.RewardBits(Constants.InfectedBitReward);

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
                    SetAvatar(playerId, true);
                }
            }

            BoneMenuManager.PopulatePage();
        }

        // TODO: fix this showing "look out for them..." when they are the last survivor
        private void SurvivorNotification(PlayerID playerId)
        {
            playerId.TryGetDisplayName(out var displayName);
            string _displayName = string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName;
            string last = "look out for them...";

            if (Survivors.PlayerCount <= 1)
                last = "the last survivor has fallen...";

            MenuHelper.ShowNotification(
                "Infected",
                $"{_displayName} is now infected, {last} ({Survivors.PlayerCount} survivors left)",
                4f);
        }

        private IEnumerator InfectedLookingWait()
        {
            int remaining = Config.CountdownLength.Value;

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

            if (player.IsMe)
            {
                StatsManager.ApplyStats();
                team?.Metadata.CanUseGunsChanged();
            }

            if (InitialTeam)
            {
                if (!player.IsMe)
                    return;

                HandleInitialTeam(team);
            }
            else if (team == InfectedChildren)
            {
                if (player.IsMe)
                    MenuHelper.ShowNotification("Infected", "Oh no, you got infected! Now you have to infect others...", 4f);
                else if (!player.IsMe)
                    SurvivorNotification(player);
            }
        }

        private void HandleInitialTeam(InfectionTeam team)
        {
            if (!InitialTeam)
                return;

            InitialTeam = false;

            if (team != Infected)
                MenuHelper.ShowNotification("Survivor", "You got lucky! Make sure you don't get infected!", 3);

            if (!InfectedLooking.GetValue())
                VisionManager.HideVisionAndReveal(team != Infected ? 3 : 0);
        }

        private float _elapsedTimeMenu;

        protected override void OnUpdate()
        {
            _elapsedTime += TimeUtilities.DeltaTime;
            // HACK: There's a better way to do this, but for some fucking reason it doesnt want to cooperate. This must do for now.
            // TODO: Change this shit.
            if (NetworkInfo.IsHost)
            {
                _elapsedTimeMenu += TimeUtilities.DeltaTime;

                if (_elapsedTimeMenu >= 1f)
                {
                    _elapsedTimeMenu = 0f;
                    TeamManager.InfectedTeams.ForEach(x => GamemodeMenuManager.FormatApplyName(x, true));
                }
            }

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

                PointItemManager.RewardBits(Constants.SurvivorsBitReward);
            }
        }

        public override bool CheckReadyConditions()
        {
#if !DEBUG && !SOLOTESTING
            if (NetworkPlayer.Players.Count < 2)
            {
                Core.Logger.Error("There must be at least 2 players to start");
                return false;
            }

            if (Config.SelectMode.Value == AvatarSelectMode.CONFIG)
            {
                var selected = new Barcode(Config.SelectedAvatar.Value.Barcode);

                if (string.IsNullOrWhiteSpace(Config.SelectedAvatar.Value.Barcode))
                {
                    Core.Logger.Error("No avatar selected while in CONFIG mode");
                    return false;
                }

                if (selected?.IsValid() != true || selected?.IsValidSize() != true)
                {
                    Core.Logger.Error("Avatar selected while in CONFIG mode is not valid");
                    return false;
                }
            }

            if (Config.ChildrenSelectedAvatar.Enabled && Config.ChildrenSelectMode.Value == ChildrenAvatarSelectMode.CONFIG)
            {
                var selected = new Barcode(Config.ChildrenSelectedAvatar.Value.Barcode);

                if (string.IsNullOrWhiteSpace(Config.ChildrenSelectedAvatar.Value.Barcode))
                {
                    Core.Logger.Error("No children avatar selected while in CONFIG mode");
                    return false;
                }

                if (selected?.IsValid() != true || selected?.IsValidSize() != true)
                {
                    Core.Logger.Error("Children Avatar selected while in CONFIG mode is not valid");
                    return false;
                }
            }

            if (NetworkPlayer.Players.Count > Config.InfectedCount.Value)
            {
                Core.Logger.Error("There must be at least one survivor");
                return false;
            }

            if (CountPlayersThatHaveAvatarInfection() > Config.InfectedCount.Value)
            {
                Core.Logger.Error($"There must be at least {Config.InfectedCount.Value} people with AvatarInfection");
                return false;
            }
#endif
            return true;
        }

        public override void OnGamemodeReady()
            => IHaveAvatarInfection();

        public override void OnGamemodeSelected()
            => IHaveAvatarInfection();

        public override bool CanAttack(PlayerID player)
        {
            if (!IsStarted)
                return true;

            if (Config.FriendlyFire.Value)
                return true;

            var playerTeam = TeamManager.GetPlayerTeam(player);
            var localTeam = TeamManager.GetLocalTeam();
            return playerTeam != localTeam && (!IsInfected(playerTeam) || !IsInfected(TeamManager.GetLocalTeam()));
        }

        internal void SetRandomAvatar()
            => Config.SetAvatar(GetRandomAvatar(), PlayerIDManager.LocalID);

        internal void SetRandomChildrenAvatar()
            => Config.SetChildrenAvatar(GetRandomAvatar(), PlayerIDManager.LocalID);

        internal static string GetRandomAvatar()
            => GetAvatars().Random();

        internal static string[] GetAvatars()
        {
            var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            avatars.RemoveAll(
                (Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));
            avatars.RemoveAll(
                (Il2CppSystem.Predicate<AvatarCrate>)(x => CrateFilterer.GetModID(x.Pallet) == -1));
            return [.. avatars.ToArray().Select(x => x.Barcode.ID)];
        }

        private void ApplyGamemodeSettings()
        {
            if (Config.SelectMode.Value == AvatarSelectMode.RANDOM)
                SetRandomAvatar();

            if (Config.ChildrenSelectMode.Value == ChildrenAvatarSelectMode.RANDOM)
                SetRandomChildrenAvatar();

            CountdownValue.SetValue(Config.CountdownLength.Value);

            TeamManager.InfectedTeams.ForEach(x => x.Metadata.ApplyConfig());

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

            PlayList.SetPlaylist(AudioReference.CreateReferences(Constants.Tracks));
            PlayList.Shuffle();

            RevertToDefault();

            if (NetworkInfo.IsHost)
            {
                LobbyInfoManager.LobbyInfo.Knockout = false;
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

                if (Config.UseDeathmatchSpawns.Value)
                    UseDeathmatchSpawns_Init(!Config.TeleportOnStart.Value);
                else
                    ClearDeathmatchSpawns();
            });

            if (NetworkInfo.IsHost)
                GamemodeMenuManager.RefreshSettingsPage();
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
            else if (Config.TeleportOnEnd.Value)
                TeleportToHost();

            if (NetworkInfo.IsHost)
                GamemodeMenuManager.RefreshSettingsPage();
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
            var last = new List<ulong>(LastInfected);
            LastInfected.Clear();
            var players = new List<PlayerID>(PlayerIDManager.PlayerIDs);
            players.Shuffle();

            bool selected = false;
            int selectedNum = 0;
            int failSafe = 0;

            var _last = new List<PlayerID>(players);
            _last.RemoveAll(x => last.Contains(x.PlatformID));
            if (_last.Count < Config.InfectedCount.Value)
                last.Clear();
            else
                players.RemoveAll(x => last.Contains(x.PlatformID));

            while (failSafe < 1000)
            {
                failSafe++;
                if (selectedNum >= Config.InfectedCount.Value)
                    break;

                var player = players.Random();
                if (!DoYouHaveAvatarInfection(player))
                    continue;

                selectedNum++;

#if SOLOTESTING
                var team = InfectedChildren;
#else
                var team = Infected;
#endif
                TeamManager.TryAssignTeam(player, team);
                bool exists = NetworkPlayerManager.TryGetPlayer(player.SmallID, out NetworkPlayer plr) && plr.HasRig;

                if (Config.SelectMode.Value == AvatarSelectMode.FIRST_INFECTED && !selected && exists)
                {
                    var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                    if (!string.IsNullOrWhiteSpace(avatar))
                    {
                        selected = true;
                        Config.SetAvatar(avatar, plr.PlayerID);
                    }
                }

                SetAvatar(player, team == InfectedChildren);
                players.Remove(player);
                LastInfected.Add(player.PlatformID);
            }

            players.ForEach(x => TeamManager.TryAssignTeam(x, Survivors));
        }

        private void SetAvatar(PlayerID player, bool isChildren)
        {
            if (!isChildren || (isChildren && !Config.ChildrenSelectedAvatar.Enabled))
                EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(player.PlatformID, Config.SelectedAvatar.Value?.Barcode, Config.SelectedAvatar.Value?.Origin ?? -1));
            else
                EventManager.TryInvokeEvent(EventType.SwapAvatar, new SwapAvatarData(player.PlatformID, Config.ChildrenSelectedAvatar.Value?.Barcode, Config.ChildrenSelectedAvatar.Value?.Origin ?? -1));
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
                EventManager.TryInvokeEvent(EventType.PlayerInfected, new PlayerInfectedData(player.PlatformID, -1));
        }

        private void KilledEvent(PlayerID player, PlayerID killer)
        {
            if (killer == null || Config.InfectType.Value != InfectType.DEATH)
                return;

            if (TeamManager.GetPlayerTeam(player) == Survivors && IsPlayerInfected(killer))
                EventManager.TryInvokeEvent(EventType.PlayerInfected, new PlayerInfectedData(player.PlatformID, (long)killer.PlatformID));
        }

        public enum InfectType
        {
            TOUCH,
            DEATH
        }

        public enum AvatarSelectMode
        {
            CONFIG = 0,
            FIRST_INFECTED = 1,
            RANDOM = 2
        }

        public enum ChildrenAvatarSelectMode
        {
            CONFIG = 0,
            RANDOM = 2
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

    internal class SwapAvatarData(ulong target, string barcode, long origin = -1)
    {
        public ulong Target { get; set; } = target;

        public string Barcode { get; set; } = barcode;

        public long Origin { get; set; } = origin;
    }

    internal class PlayerInfectedData(ulong userId, long by)
    {
        public ulong UserId { get; set; } = userId;

        public long By { get; set; } = by;
    }
}