// Ignore Spelling: Metadata Unragdoll

using System;
using System.Collections;
using System.Collections.Generic;

using AvatarInfection.Helper;
using AvatarInfection.Managers;
using AvatarInfection.Settings;
using AvatarInfection.Utilities;

#if RELEASE
using Il2CppSLZ.Marrow.Warehouse;
#endif

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

        internal InfectionSettings Config { get; set; }

        public bool OneMinuteLeft { get; private set; }

        private readonly Dictionary<PlayerID, PlayerActionType> LastPlayerActions = [];

        private int _surivorsLastCheckedMinutes;

        private static bool appliedDeathmatchSpawns;

        private bool WasStarted;

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
            MultiplayerHooking.OnStartedServer += MetadataManager.SetAllMetadata;
            MultiplayerHooking.OnJoinedServer += MetadataManager.SetAllMetadata;
            MultiplayerHooking.OnTargetLevelLoaded += MetadataManager.SetAllMetadata;
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
            // Fired after team is assigned, not before
            TeamManager.OnAssignedToInfectedTeam += OnAssignedToTeam;

            InfectedLooking = new MetadataBool(nameof(InfectedLooking), Metadata);
            CountdownValue = new MetadataInt(nameof(CountdownValue), Metadata);
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

            if (key == nameof(InfectedLooking) && InfectedLooking.GetValue())
                InfectedLookingEvent();
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

        protected override void OnUpdate()
        {
            _elapsedTime += TimeUtilities.DeltaTime;
            if (NetworkInfo.IsHost && !Config.NoTimeLimit.Value)
            {
                if (!OneMinuteLeft && Config.TimeLimit.Value - ElapsedMinutes == 1)
                    EventManager.TryInvokeEvent(EventType.OneMinuteLeft);

                if (ElapsedMinutes >= Config.TimeLimit.Value)
                {
                    EventManager.TryInvokeEvent(EventType.SurvivorsVictory);
                    GamemodeManager.StopGamemode();
                }
            }

            if (Instance.IsStarted && TeamManager.GetLocalTeam() == Survivors)
                SurvivorsUpdate();
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

            if (Config.SelectedAvatar.Value?.SelectMode == AvatarSelectMode.CONFIG && !AvatarConditions(Config.SelectedAvatar?.Value?.Barcode, "children"))
                return false;

            if (Config.ChildrenSelectedAvatar.Enabled
                && Config.ChildrenSelectedAvatar.Value?.SelectMode == AvatarSelectMode.CONFIG
                && !AvatarConditions(Config.ChildrenSelectedAvatar?.Value?.Barcode, "children"))
            {
                return false;
            }

            if (NetworkPlayer.Players.Count <= Config.InfectedCount.Value)
            {
                Core.Logger.Error($"There must be at least {Config.InfectedCount.Value + 1} player(s) to start");
                return false;
            }

            if (MetadataManager.CountPlayersWithAvatarInfection() <= Config.InfectedCount.Value)
            {
                Core.Logger.Error($"There must be at least {Config.InfectedCount.Value + 1} player(s) with AvatarInfection installed");
                return false;
            }
#endif
            return true;
        }

#if !DEBUG && !SOLOTESTING
        private static bool AvatarConditions(string barcode, string prefix = "")
        {
            var selected = new Barcode(barcode);

            if (string.IsNullOrWhiteSpace(barcode))
            {
                Core.Logger.Error($"No{(!string.IsNullOrWhiteSpace(prefix) ? $" {prefix} " : string.Empty)}avatar selected while in CONFIG mode");
                return false;
            }

            if (selected?.IsValid() != true || selected?.IsValidSize() != true)
            {
                Core.Logger.Error($"{(!string.IsNullOrWhiteSpace(prefix) ? $"{FirstCharToUpper(prefix)} " : string.Empty)}Avatar selected while in CONFIG mode is not valid");
                return false;
            }

            if (!AssetWarehouse.Instance.TryGetCrate(selected, out Crate crate))
            {
                Core.Logger.Error($"{(!string.IsNullOrWhiteSpace(prefix) ? $"{FirstCharToUpper(prefix)} " : string.Empty)}Avatar selected while in CONFIG mode is not installed");
                return false;
            }

            if (!crate.IsPublic())
            {
                Core.Logger.Error($"{(!string.IsNullOrWhiteSpace(prefix) ? $"{FirstCharToUpper(prefix)} " : string.Empty)}Modded avatar selected while in CONFIG mode is not public");
                return false;
            }
            return true;
        }

        private static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return $"{char.ToUpper(input[0])}{input[1..]}";
        }
#endif

        public override void OnGamemodeReady()
            => MetadataManager.SetAllMetadata();

        public override void OnGamemodeSelected()
            => MetadataManager.SetAllMetadata();

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

        private void ApplyGamemodeSettings()
        {
            AvatarSetting(AvatarSelectMode.RANDOM, (setting) => setting.SetRandomAvatar());

            CountdownValue.SetValue(Config.CountdownLength.Value);

            TeamManager.InfectedTeams.ForEach(x => x.Metadata.ApplyConfig());

            var now = DateTimeOffset.Now;
            Config.StartUnix.Value = now.ToUnixTimeMilliseconds();
            if (!Config.NoTimeLimit.Value)
                Config.EndUnix.Value = now.AddMinutes(Config.TimeLimit.Value).ToUnixTimeMilliseconds();
            else
                Config.EndUnix.Value = -1;

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
            if (!WasStarted)
                return;

            BoneMenuManager.PopulatePage();

            RevertToDefault(false);

            PlayList.StopPlaylist();

            ClearDeathmatchSpawns();

            StatsManager.ClearOverrides();

            FusionOverrides.ForceUpdateOverrides();
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
                // This could be easily exploited to avoid being the infected by using a modified version of the mod
                // If such issues will occur, this will be changed
                // If you are reading, please don't do that, this does not intend to bring ideas :(
                if (!MetadataManager.DoYouHaveAvatarInfection(player))
                    continue;

                selectedNum++;

#if SOLOTESTING
                var team = InfectedChildren;
#else
                var team = Infected;
#endif
                TeamManager.TryAssignTeam(player, team);
                if (!selected)
                    selected = TrySetFirstInfected(player);

                SetAvatar(player, team == InfectedChildren);
                players.Remove(player);
                LastInfected.Add(player.PlatformID);
            }

            if (!selected)
                AvatarSetting(AvatarSelectMode.FIRST_INFECTED, (setting) => setting.SetRandomAvatar());

            players.ForEach(x => TeamManager.TryAssignTeam(x, Survivors));
        }

        private bool TrySetFirstInfected(PlayerID player)
        {
            bool exists = NetworkPlayerManager.TryGetPlayer(player.SmallID, out NetworkPlayer plr) && plr.HasRig;

            if (exists)
            {
                var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                if (!string.IsNullOrWhiteSpace(avatar) && player.IsAvatarDownloadable())
                {
                    AvatarSetting(AvatarSelectMode.FIRST_INFECTED, (setting) => setting.SetAvatar(avatar, player));
                    return true;
                }
            }
            return false;
        }

        private void AvatarSetting(AvatarSelectMode target, Action<AvatarSetting> callback)
        {
            foreach (var setting in Config._settingsList)
            {
                if (setting is AvatarSetting avatarSetting && avatarSetting.Value?.SelectMode == target)
                    callback?.Invoke(avatarSetting);
            }
        }

        private void SetAvatar(PlayerID player, bool isChildren)
        {
            if (!isChildren || (isChildren && !Config.ChildrenSelectedAvatar.Enabled))
                EventManager.TryInvokeEvent(EventType.SwapAvatar, SwapAvatarData.Create(player, Config.SelectedAvatar));
            else
                EventManager.TryInvokeEvent(EventType.SwapAvatar, SwapAvatarData.Create(player, Config.ChildrenSelectedAvatar));
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
            if (killer?.IsValid != true || Config.InfectType.Value != InfectType.DEATH)
                return;

            if (TeamManager.GetPlayerTeam(player) == Survivors && IsPlayerInfected(killer))
                EventManager.TryInvokeEvent(EventType.PlayerInfected, new PlayerInfectedData(player.PlatformID, (long)killer.PlatformID));
        }

        public enum InfectType
        {
            TOUCH = 0,
            DEATH = 1
        }

        public enum EventType
        {
            PlayerInfected = 0,
            SwapAvatar = 1,
            RefreshStats = 2,
            TeleportToHost = 3,
            OneMinuteLeft = 4,
            InfectedVictory = 5,
            SurvivorsVictory = 6
        }
    }

    internal class SwapAvatarData(ulong target, string barcode, long origin = -1)
    {
        public ulong Target { get; set; } = target;

        public string Barcode { get; set; } = barcode;

        public long Origin { get; set; } = origin;

        public static SwapAvatarData Create(PlayerID player, AvatarSetting setting)
            => new(player.PlatformID, setting.Value?.Barcode, setting.Value?.Origin ?? -1);
    }

    internal class PlayerInfectedData(ulong userId, long by)
    {
        public ulong UserId { get; set; } = userId;

        public long By { get; set; } = by;
    }
}