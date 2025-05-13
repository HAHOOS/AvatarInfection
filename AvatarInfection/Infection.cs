// Ignore Spelling: Metadata Unragdoll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Bonelab;

using LabFusion.Menu.Data;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;
using LabFusion.Entities;
using LabFusion.SDK.Triggers;
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
using BoneLib.BoneMenu;

using AvatarInfection.Utilities;
using AvatarInfection.Helper;

using LabFusion.RPC;
using LabFusion.Preferences.Client;
using LabFusion.Downloading;
using LabFusion.Data;

namespace AvatarInfection
{
    public class Infection : Gamemode
    {
        public override string Title => "Avatar Infection";

        public override string Author => "HAHOOS";

        public override string Barcode => Defaults.Barcode;

        public override Texture Logo => Core.Icon;

        public override bool DisableSpawnGun => _DisableSpawnGun.ClientValue;

        public override bool DisableDevTools => _DisableDevTools.ClientValue;

        public override bool AutoHolsterOnDeath => true;

        public override bool DisableManualUnragdoll => true;

        public override bool AutoStopOnSceneLoad => true;

        public override bool ManualReady => false;
#pragma warning disable IDE1006 // Naming Styles
        internal ServerSetting<bool> _DisableSpawnGun { get; private set; }

        internal ServerSetting<bool> _DisableDevTools { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static Infection Instance { get; private set; }

        internal static Team Infected { get; } = new("Infected");

        internal static TeamMetadata InfectedMetadata;

        internal static Team Survivors { get; } = new("Survivors");

        internal static TeamMetadata SurvivorsMetadata;

        internal static Team InfectedChildren { get; } = new("InfectedChildren");

        internal static TeamMetadata InfectedChildrenMetadata;

        internal static TeamManager TeamManager { get; } = new();

        private const string TeamConfigName = "{0} Config";

        internal static class Defaults
        {
            public const string Barcode = "HAHOOS.AvatarInfection";

            public const int TimeLimit = 10;

            public const int InfectedBitReward = 50;

            public const int SurvivorsBitReward = 75;

            public const bool UntilAllFound = false;

            public readonly static TeamConfig InfectedStats = new()
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

            public readonly static TeamConfig InfectedChildrenStats = new()
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

            public readonly static TeamConfig SurvivorsStats = new()
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

            public const bool ShouldTeleportToHost = true;

            public const int CountdownLength = 30;

            public const InfectTypeEnum InfectType = InfectTypeEnum.TOUCH;

            public const bool SuicideInfects = true;

            public const int HoldTime = 0;

            public const bool TeleportOnEnd = false;

            public const bool UseDeathMatchSpawns = true;

            public const bool SyncWithInfected = false;

            public const bool ShowCountdownToAll = false;

            public const AvatarSelectMode SelectMode = AvatarSelectMode.Config;

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

        internal static ServerSetting<string> SelectedAvatar;
        public MusicPlaylist PlayList { get; } = new();

        private bool NoTimeLimit { get; set; } = Defaults.UntilAllFound;

        internal TriggerEvent InfectEvent;
        internal TriggerEvent OneMinuteLeftEvent;
        internal TriggerEvent InfectedVictoryEvent;
        internal TriggerEvent SurvivorsVictoryEvent;
        internal static TriggerEvent RefreshStatsEvent;

        internal MetadataBool InfectedLooking;

        // In seconds
        internal int TimeLimit { get; private set; } = Defaults.TimeLimit;

        internal int InfectedCount { get; private set; } = Defaults.InfectedCount;

        internal bool InitialTeam { get; private set; } = true;

        internal bool HasBeenInfected { get; private set; } = false;

        private static ModuleLogger Logger => FusionModule.Logger;

        private float _elapsedTime = 0f;

        internal float ElapsedSeconds => _elapsedTime;
        internal int ElapsedMinutes => Mathf.FloorToInt(ElapsedSeconds / 60f);

        internal static bool HideVision { get; private set; }

        internal static TriggerEvent Teleport { get; private set; }

        internal static bool TeleportOnStart { get; private set; } = Defaults.ShouldTeleportToHost;

        internal static InfectTypeEnum InfectType { get; private set; } = Defaults.InfectType;

        internal MetadataBool AKI { get; private set; }

        internal static ServerSetting<int> CountdownLength { get; private set; }

        internal bool AllowKeepInventory { get; private set; } = Defaults.AllowKeepInventory;

        internal bool SuicideInfects { get; private set; } = Defaults.SuicideInfects;

        internal int HoldTime { get; private set; } = Defaults.HoldTime;

        internal ServerSetting<bool> TeleportOnEnd { get; private set; }

        internal ServerSetting<bool> UseDeathmatchSpawns { get; private set; }

        private List<ElementData> InfectedElements;
        private List<ElementData> SurvivorsElements;
        private List<ElementData> InfectedChildrenElements;

        internal bool SyncWithInfected { get; private set; } = Defaults.SyncWithInfected;

        internal AvatarSelectMode SelectMode { get; private set; } = Defaults.SelectMode;

        internal ServerSetting<bool> ShowCountdownToAll { get; private set; }

        internal MetadataInt CountdownValue;

        internal MetadataVariableT<long?> StartUnix { get; private set; }

        internal MetadataVariableT<long?> EndUnix { get; private set; }

        public override GroupElementData CreateSettingsGroup()
        {
            var group = new GroupElementData()
            {
                Title = Title,
            };

            if (group == null)
            {
                Logger.Error("Group is null");
                return null;
            }

            GroupElementData avatarGroup = group.AddGroup("Avatar");

            avatarGroup.AddElement("Selected Avatar: ", () =>
            {
                if (IsStarted)
                    return;

                var rigManager = Player.RigManager;
                if (rigManager?.AvatarCrate?.Barcode != null)
                {
                    var avatar = rigManager.AvatarCrate.Barcode.ID;

                    if (string.IsNullOrWhiteSpace(avatar))
                        return;
                    SelectedAvatar.ClientValue = avatar;

                    string title = !string.IsNullOrWhiteSpace(rigManager.AvatarCrate?.Scannable?.Title) ? rigManager.AvatarCrate.Scannable.Title : "N/A";

                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                        avatarGroup.Title,
                        "Selected Avatar:",
                        (element) => element.Title = $"Selected Avatar: {title}",
                        true);
                }
            });

            avatarGroup.AddElement("Select Mode", SelectMode, Instance, (val) => SelectMode = (AvatarSelectMode)val);

            group.AddElement(CreateElementsForTeam(TeamEnum.Infected));

            group.AddElement(CreateElementsForTeam(TeamEnum.InfectedChildren));

            group.AddElement(CreateElementsForTeam(TeamEnum.Survivors));

            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", InfectedCount, (val) => InfectedCount = val, min: 1, max: 5);

            generalGroup.AddElement("Time Limit", TimeLimit, (val) =>
            {
                TimeLimit = val;
                if (IsStarted)
                    EndUnix.SetValue(DateTimeOffset.FromUnixTimeMilliseconds((long)StartUnix.GetValue()).AddMinutes(val).ToUnixTimeMilliseconds());
            }, min: 1);

            generalGroup.AddElement("Disable Spawn Gun", DisableSpawnGun, (val) => _DisableSpawnGun.ClientValue = val);

            generalGroup.AddElement("Disable Dev Tools", DisableDevTools, (val) => _DisableDevTools.ClientValue = val);

            generalGroup.AddElement("Allow Keep Inventory", AllowKeepInventory, (val) =>
            {
                AllowKeepInventory = val;
                if (IsStarted)
                    AKI.SetValue(val);
            });

            generalGroup.AddElement("No Time Limit", NoTimeLimit, (val) =>
            {
                NoTimeLimit = val;
                if (IsStarted)
                    EndUnix.SetValue(-1);
            });

            generalGroup.AddElement("Use Deathmatch Spawns", UseDeathmatchSpawns.ClientValue, (val) => UseDeathmatchSpawns.ClientValue = val);

            generalGroup.AddElement("Teleport To Host On Start", TeleportOnStart, (val) => TeleportOnStart = val);

            generalGroup.AddElement("Teleport To Host On End", TeleportOnEnd.ClientValue, (val) => TeleportOnEnd.ClientValue = val);

            generalGroup.AddElement("Countdown Length", CountdownLength.ClientValue, (val) => CountdownLength.ClientValue = val, 5, 0, 3600);
            generalGroup.AddElement("Show Countdown to All Players", ShowCountdownToAll.ClientValue, (val) => ShowCountdownToAll.ClientValue = val);

            generalGroup.AddElement("Infect Type", InfectType, Instance, (val) => InfectType = (InfectTypeEnum)val);

            generalGroup.AddElement("Suicide Infects", SuicideInfects, (val) => SuicideInfects = val);
            generalGroup.AddElement("Hold Time (Touch Infect Type)", HoldTime, (val) => HoldTime = val, max: 60);

            return group;
        }

        public enum AvatarSelectMode
        {
            Config,
            FirstInfected,
            Random
        }

        private GroupElementData CreateElementsForTeam(TeamEnum team)
        {
            var metadata = team == TeamEnum.Infected ? InfectedMetadata : team == TeamEnum.Survivors ? SurvivorsMetadata : InfectedChildrenMetadata;
            var group = new GroupElementData()
            {
                Title = $"{(team == TeamEnum.Survivors ? "Survivors Stats"
                   : team == TeamEnum.InfectedChildren ? "Infected Children Stats" : "Infected Stats")}"
            };

            void applyButtonUpdate()
            {
                const string name = "Apply new settings";
                var metadata = team == TeamEnum.Infected ? InfectedMetadata : team == TeamEnum.Survivors ? SurvivorsMetadata : InfectedChildrenMetadata;
                var remote = metadata.GetConfigFromMetadata();
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    string.Format(TeamConfigName, Infected.DisplayName),
                    "Apply new settings", (el) => el.Title = remote != metadata.Config ? $"<color=#FF0000>{name}</color>" : name, false);
            }

            group.AddElement("Apply new settings (use when the gamemode is already started)", () =>
            {
                if (IsStarted)
                {
                    var _metadata = metadata;
                    if (team == TeamEnum.InfectedChildren && SyncWithInfected)
                        _metadata = InfectedMetadata;

                    var remote = _metadata.GetConfigFromMetadata();
                    if (remote == _metadata.Config)
                        return;
                    _metadata.ApplyConfig();
                    RefreshStatsEvent?.TryInvoke(team.ToString());
                }
            });
            group.AddElement("Mortality", metadata.Config.Mortality, (val) => { metadata.Config.Mortality = val; applyButtonUpdate(); });
            group.AddElement("Can Use Guns", metadata.Config.CanUseGuns, (val) => { metadata.Config.CanUseGuns = val; applyButtonUpdate(); });
            group.AddElement("Override Vitality", metadata.Config.Vitality_Enabled, (val) => { metadata.Config.Vitality_Enabled = val; applyButtonUpdate(); });
            group.AddElement("Vitality", metadata.Config.Vitality, (val) => { metadata.Config.Vitality = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Speed", metadata.Config.Speed_Enabled, (val) => { metadata.Config.Speed_Enabled = val; applyButtonUpdate(); });
            group.AddElement("Speed", metadata.Config.Speed, (val) => { metadata.Config.Speed = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Agility", metadata.Config.Agility_Enabled, (val) => { metadata.Config.Agility_Enabled = val; applyButtonUpdate(); });
            group.AddElement("Agility", metadata.Config.Agility, (val) => { metadata.Config.Agility = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Strength Upper", metadata.Config.StrengthUpper_Enabled, (val) => { metadata.Config.StrengthUpper_Enabled = val; applyButtonUpdate(); });
            group.AddElement("Strength Upper", metadata.Config.StrengthUpper, (val) => { metadata.Config.StrengthUpper = val; applyButtonUpdate(); }, increment: Increment);

            // Borrowed from Infect Type Enum element thingy which initially borrowed from BoneLib which then borrowed from LabFusion
            var increment = new FunctionElementData()
            {
                Title = $"Increment: {Increment}",
                OnPressed = () =>
                {
                    var group = string.Format(TeamConfigName, metadata.Team.DisplayName);

                    IncrementIndex++;
                    IncrementIndex %= IncrementValues.Count;
                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                        group,
                        "Increment:",
                        (el) => el.Title = $"Increment: {Increment}");

                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, "Strength Upper", (el) => el.Increment = Increment);
                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, "Agility", (el) => el.Increment = Increment);
                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, "Jump Power", (el) => el.Increment = Increment);
                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, "Speed", (el) => el.Increment = Increment);
                    Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, "Vitality", (el) => el.Increment = Increment);
                }
            };
            group.AddElement(increment);

            if (team == TeamEnum.InfectedChildren)
                group.AddElement("Sync with Infected", SyncWithInfected, (val) => SyncWithInfected = val);

            return group;
        }

        private enum TeamEnum
        {
            Survivors,
            InfectedChildren,
            Infected
        }

        internal float Increment
        {
            get
            {
                return IncrementValues[IncrementIndex];
            }
        }

        private readonly IReadOnlyList<float> IncrementValues = [0.2f, 0.5f, 1f, 5f];
        private int IncrementIndex = 0;

        private static BoneLib.BoneMenu.Page ModPage { get; set; }

        public override void OnGamemodeRegistered()
        {
            Instance = this;
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

            _DisableDevTools = new(Instance, nameof(DisableDevTools), Defaults.DisableDevTools);
            _DisableSpawnGun = new(Instance, nameof(DisableSpawnGun), Defaults.DisableSpawnGun);

            InfectedMetadata = new TeamMetadata(Infected, Metadata, new TeamConfig(Defaults.InfectedStats));
            SurvivorsMetadata = new TeamMetadata(Survivors, Metadata, new TeamConfig(Defaults.SurvivorsStats));
            InfectedChildrenMetadata = new TeamMetadata(InfectedChildren, Metadata, new TeamConfig(Defaults.InfectedChildrenStats));

            SelectedAvatar = new(Instance, nameof(SelectedAvatar), null);
            SelectedAvatar.OnValueChanged += SelectedPlayerOverride;

            CountdownLength = new(Instance, nameof(CountdownLength), Defaults.CountdownLength);

            AKI = new MetadataBool("AllowKeepInventory", Metadata);

            InfectedLooking = new(nameof(InfectedLooking), Metadata);

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

            CountdownValue = new MetadataInt(nameof(CountdownValue), Metadata);

            StartUnix = new MetadataVariableT<long?>(nameof(StartUnix), Metadata);

            EndUnix = new MetadataVariableT<long?>(nameof(EndUnix), Metadata);

            Metadata.OnMetadataChanged += OnMetadataChanged;

            InfectEvent = new TriggerEvent("InfectPlayer", Relay, true);
            InfectEvent.OnTriggeredWithValue += PlayerInfected;

            OneMinuteLeftEvent = new TriggerEvent("OneMinuteLeft", Relay, true);
            OneMinuteLeftEvent.OnTriggered += OneMinuteLeft;

            InfectedVictoryEvent = new TriggerEvent("InfectedVictory", Relay, true);
            InfectedVictoryEvent.OnTriggered += InfectedVictory;

            SurvivorsVictoryEvent = new TriggerEvent("SurvivorsVictory", Relay, true);
            SurvivorsVictoryEvent.OnTriggered += SurvivorsVictory;

            RefreshStatsEvent = new TriggerEvent("RefreshEvent", Relay, true);
            RefreshStatsEvent.OnTriggeredWithValue += RefreshStats;

            Teleport = new TriggerEvent("TeleportToHost", Relay, true);
            Teleport.OnTriggered += TeleportToHost;

            var authorPage = BoneLib.BoneMenu.Page.Root.CreatePage("HAHOOS", Color.white);
            ModPage = authorPage.CreatePage("AvatarInfection", Color.magenta);
            PopulatePage();

            MultiplayerHooking.OnDisconnect += PopulatePage;
            MultiplayerHooking.OnJoinServer += PopulatePage;
            MultiplayerHooking.OnPlayerJoin += Hook;
            MultiplayerHooking.OnPlayerLeave += Hook;
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

            InfectedElements = null;
            SurvivorsElements = null;
            InfectedChildrenElements = null;

            TeamManager.Unregister();

            Metadata.OnMetadataChanged -= OnMetadataChanged;

            InfectEvent?.UnregisterEvent();
            InfectEvent = null;

            OneMinuteLeftEvent?.UnregisterEvent();
            OneMinuteLeftEvent = null;

            InfectedVictoryEvent?.UnregisterEvent();
            InfectedVictoryEvent = null;

            SurvivorsVictoryEvent?.UnregisterEvent();
            SurvivorsVictoryEvent = null;

            RefreshStatsEvent?.UnregisterEvent();
            RefreshStatsEvent = null;

            Teleport?.UnregisterEvent();
            Teleport = null;

            Menu.DestroyPage(ModPage);
            ModPage = null;
            MultiplayerHooking.OnDisconnect -= PopulatePage;
            MultiplayerHooking.OnJoinServer -= PopulatePage;
            MultiplayerHooking.OnPlayerJoin -= Hook;
            MultiplayerHooking.OnPlayerLeave -= Hook;
        }

        private void OnPlayerLeave(PlayerId id)
        {
            if (!NetworkInfo.IsServer)
                return;

            if (!IsStarted)
                return;

            if (Infected.PlayerCount == 0 && InfectedChildren.PlayerCount == 0)
            {
                InfectedVictoryEvent.TryInvoke();
                GamemodeManager.StopGamemode();
            }
            else if (Survivors.PlayerCount == 0)
            {
                SurvivorsVictoryEvent.TryInvoke();
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

        private void Hook(PlayerId _) => PopulatePage();

        private void PopulatePage()
        {
            if (ModPage == null)
                return;

            ModPage.RemoveAll();
            ModPage.CreateFunction("Refresh", Color.cyan, PopulatePage);
            var seperator = ModPage.CreateFunction("[===============]", Color.magenta, null);
            seperator.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);

            if (!NetworkInfo.HasServer)
            {
                var label = ModPage.CreateFunction("You aren't in any server :(", Color.white, null);
                label.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);
                return;
            }

            if (!IsStarted)
            {
                var label = ModPage.CreateFunction("Gamemode is not started :(", Color.white, null);
                label.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);
                return;
            }

            Dictionary<PlayerId, Team> Teams = [];

            foreach (var player in PlayerIdManager.PlayerIds)
            {
                var team = TeamManager.GetPlayerTeam(player);
                Teams.Add(player, team);
            }

            var infected = Teams.Any(x => x.Value == Infected) ?
                ModPage.CreatePage($"Infected ({Teams.Count(x => x.Value == Infected)})", Color.green) : null;
            var children = Teams.Any(x => x.Value == InfectedChildren) ?
                ModPage.CreatePage($"Infected Children ({Teams.Count(x => x.Value == InfectedChildren)})", new Color(0, 1, 0)) : null;
            var survivors = Teams.Any(x => x.Value == Survivors) ?
                ModPage.CreatePage($"Survivors ({Teams.Count(x => x.Value == Survivors)})", Color.cyan) : null;

            var unidentified = Teams.Any(x => x.Value == null) ?
                ModPage.CreatePage($"Unidentified ({Teams.Count(x => x.Value == null)})", Color.gray) : null;

            foreach (var team in Teams)
            {
                if (!team.Key.TryGetDisplayName(out var displayName))
                    continue;

                var page = team.Value == Infected
                    ? infected : team.Value == InfectedChildren
                    ? children : team.Value == Survivors
                    ? survivors : unidentified;
                if (page == null)
                    continue;

                page.CreateFunction(displayName, Color.white, null);
            }
        }

        private void RefreshStats(string isInfectedTeam)
        {
            bool success = Enum.TryParse(isInfectedTeam, out TeamEnum team);
            if (!success)
                return;

            if (TeamManager.GetLocalTeam() == (team == TeamEnum.Infected ? Infected : team == TeamEnum.Survivors ? Survivors : InfectedChildren))
                SetStats();
        }

        private void InfectedLookingEvent()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() != Infected && TeamManager.GetLocalTeam() != InfectedChildren)
            {
                FusionNotifier.Send(new FusionNotification()
                {
                    Title = "Run...",
                    Message = "The infected have awaken... you have to run... save yourselves.. please",
                    PopupLength = 5f,
                    SaveToMenu = false,
                    ShowPopup = true,
                    Type = NotificationType.WARNING
                });
            }
        }

        private void PlayerInfected(string stringID)
        {
            if (!IsStarted)
                return;

            if (!ulong.TryParse(stringID, out ulong userId))
                return;

            var playerId = PlayerIdManager.GetPlayerId(userId);

            if (playerId == null)
                return;

            if (NetworkInfo.IsServer && Survivors.HasPlayer(playerId))
            {
                if (Survivors.PlayerCount <= 1)
                {
                    InfectedVictoryEvent.TryInvoke();
                    GamemodeManager.StopGamemode();
                }
                else
                {
                    TeamManager.TryAssignTeam(playerId, InfectedChildren);
                }
            }

            if (playerId.IsMe && !HasBeenInfected)
            {
                if (Survivors.PlayerCount > 1)
                    SwapAvatar(SelectedAvatar.ClientValue);

                FusionNotifier.Send(new FusionNotification()
                {
                    ShowPopup = true,
                    Title = "Infected",
                    Message = "Oh no, you got infected! Now you have to infect others...",
                    PopupLength = 4,
                    Type = NotificationType.INFORMATION
                });

                HasBeenInfected = true;
            }
            else if (!playerId.IsMe)
            {
                playerId.TryGetDisplayName(out var displayName);

                FusionNotifier.Send(new FusionNotification()
                {
                    ShowPopup = true,
                    Title = "Infected",
                    Message = $"{(string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName)} is now infected, {(Survivors.PlayerCount > 1 ? "look out for them..." : "the last survivor has fallen...")}",
                    PopupLength = 4,
                    Type = NotificationType.INFORMATION
                });
            }

            PopulatePage();
        }

        private static void SwapAvatar(string barcode, ModResult downloadResult = ModResult.SUCCEEDED)
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
            int target = CountdownLength.ClientValue;

            float remaining = target;

            while (remaining > 0)
            {
                remaining -= TimeUtilities.DeltaTime;
                var round = (int)MathF.Round(remaining);
                if (CountdownValue.GetValue() != round)
                    CountdownValue.SetValue(round);
                yield return null;
            }

            InfectedLooking.SetValue(true);
        }

        private void OnAssignedToTeam(PlayerId player, Team team)
        {
            FusionOverrides.ForceUpdateOverrides();
            PopulatePage();

            if (team == null || player?.IsValid != true)
                return;

            if (!player.IsMe)
                return;

            SetStats();
            var config = team == Infected ? InfectedMetadata : team == Survivors ? SurvivorsMetadata : team == InfectedChildren ? InfectedChildrenMetadata : null;
            config?.CanUseGunsChanged();
            if (team != Survivors)
            {
                var rig = Player.RigManager;
                var PCDs = rig.gameObject.GetComponentsInChildren<PullCordDevice>();
                PCDs.ForEach(x => x._bodyLogEnabled = false);
            }
            else
            {
                var rig = Player.RigManager;
                var PCDs = rig.gameObject.GetComponentsInChildren<PullCordDevice>();
                PCDs.ForEach(x => x._bodyLogEnabled = true);
            }

            if (!InitialTeam)
                return;

            InitialTeam = false;

            if (team == Infected)
                SwapAvatar(SelectedAvatar.ClientValue);
            else
                SendNotif("Survivor", "Woah! You got lucky. Make sure you don't get infected!", 3);

            if (!InfectedLooking.GetValue())
                MelonCoroutines.Start(HideVisionAndReveal(team != Infected ? 3 : 0));
        }

        public static void SendNotif
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

        private void OneMinuteLeft()
        {
            if (!IsStarted)
                return;

            SendNotif("Avatar Infection", "One minute left!", 3.5f);
        }

        private void InfectedVictory()
        {
            if (!IsStarted)
                return;

            SendNotif("Infected Won", "Everyone has been infected!", 4f);
        }

        private void SurvivorsVictory()
        {
            if (!IsStarted)
                return;

            SendNotif("Survivors Won", "There were people not infected in time!", 4f);
        }

        private IEnumerator HideVisionAndReveal(float delaySeconds = 0)
        {
            if (IsStarted)
            {
                if ((ShowCountdownToAll.ClientValue && TeamManager.GetLocalTeam() != Infected) || TeamManager.GetLocalTeam() == Infected)
                {
                    try
                    {
                        if (CountdownLength.ClientValue != 0 && CountdownValue.GetValue() != 0 && !InfectedLooking.GetValue())
                        {
                            if (delaySeconds > 0)
                                yield return new WaitForSeconds(delaySeconds);

                            if (TeamManager.GetLocalTeam() == Infected)
                            {
                                FusionPlayer.SetMortality(false);

                                HideVision = true;

                                LocalVision.Blind = true;
                                LocalVision.BlindColor = Color.black;

                                //Countdown.IsHidden = false;
                                //Countdown.Opacity = 1;

                                // Lock movement so we can't move while vision is dark
                                LocalControls.LockMovement();
                            }

                            const float fadeLength = 1f;

                            float elapsed = 0f;
                            float totalElapsed = 0f;

                            int seconds = 0;

                            bool secondPassed = true;

                            var target = CountdownValue.GetValue();

                            while (seconds < target)
                            {
                                if (!IsStarted)
                                    break;

                                if (InfectedLooking.GetValue() && target > 5)
                                    break;

                                if (TeamManager.GetLocalTeam() == Infected)
                                {
                                    // Calculate fade-in
                                    float fadeStart = Mathf.Max(target - fadeLength, 0f);
                                    float fadeProgress = Mathf.Max(totalElapsed - fadeStart, 0f) / fadeLength;

                                    Color color = Color.Lerp(Color.black, Color.clear, fadeProgress);

                                    LocalVision.BlindColor = color;
                                }

                                // Check for second counter
                                if (secondPassed)
                                {
                                    int remainingSeconds = target - seconds;

                                    var icon = Texture2D.whiteTexture;
                                    var sprite = Sprite.Create(icon, new Rect(0, 0, 0, 0), new Vector2(0.5f, 0.5f), 100f);

                                    // Make sure tutorial rig and head titles are enabled

                                    var tutorialRig = TutorialRig.Instance;
                                    var headTitles = tutorialRig.headTitles;

                                    tutorialRig.gameObject.SetActive(true);
                                    headTitles.gameObject.SetActive(true);

                                    float timeToScale = Mathf.Lerp(0.05f, 0.4f, Mathf.Clamp01(target - 1f));

                                    tutorialRig.headTitles.timeToScale = timeToScale;

                                    tutorialRig.headTitles.CUSTOMDISPLAY(
                                        "Countdown, get ready...",
                                        remainingSeconds.ToString(),
                                        sprite,
                                        target);
                                    tutorialRig.headTitles.sr_element.sprite = sprite;

                                    secondPassed = false;
                                }

                                // Tick timer
                                elapsed += TimeUtilities.DeltaTime;
                                totalElapsed += TimeUtilities.DeltaTime;

                                // If a second passed, send the notification next frame
                                if (elapsed >= 1f)
                                {
                                    elapsed--;
                                    seconds++;

                                    secondPassed = true;
                                }

                                yield return null;
                            }

                            if (TeamManager.GetLocalTeam() == Infected)
                            {
                                var metadata = TeamManager.GetLocalTeam() == Infected ? InfectedMetadata : TeamManager.GetLocalTeam() == InfectedChildren ? InfectedChildrenMetadata : SurvivorsMetadata;
                                FusionPlayer.SetMortality(metadata.Config.Mortality);

                                LocalControls.UnlockMovement();

                                LocalVision.Blind = false;
                            }

                            TutorialRig.Instance.headTitles.CLOSEDISPLAY();
                        }
                        if (TeamManager.GetLocalTeam() == Infected)
                        {
                            FusionNotifier.Send(new FusionNotification()
                            {
                                ShowPopup = true,
                                Title = "Countdown Over",
                                Message = "GO AND INFECT THEM ALL!",
                                PopupLength = 3.5f,
                                Type = NotificationType.INFORMATION,
                            });
                        }
                    }
                    finally
                    {
                        HideVision = false;
                    }
                }
            }
        }

        internal bool _oneMinuteLeft = false;

        int lastTimeLimit = 0;

        protected override void OnUpdate()
        {
            if (HideVision && !LocalVision.Blind)
                LocalVision.Blind = true;

            _elapsedTime += TimeUtilities.DeltaTime;

            if (TeamManager.GetLocalTeam() == Survivors)
                SurvivorsUpdate();

            SetBodyLog(TeamManager.GetLocalTeam() == Survivors);

            if (!NoTimeLimit)
            {
                // Check for one minute left
                if (!_oneMinuteLeft && TimeLimit - ElapsedMinutes == 1)
                {
                    if (NetworkInfo.IsServer) OneMinuteLeftEvent.TryInvoke();
                    _oneMinuteLeft = true;
                }

                // Check for time limit
                if (NetworkInfo.IsServer && ElapsedMinutes >= TimeLimit)
                {
                    SurvivorsVictoryEvent?.TryInvoke();
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

            float? jumpPower = metadata.Config.JumpPower_Enabled ? metadata.Config.JumpPower : null;
            float? speed = metadata.Config.Speed_Enabled ? metadata.Config.Speed : null;
            float? agility = metadata.Config.Agility_Enabled ? metadata.Config.Agility : null;
            float? strengthUpper = metadata.Config.StrengthUpper_Enabled ? metadata.Config.StrengthUpper : null;

            FusionPlayerExtended.SetOverrides(jumpPower, speed, agility, strengthUpper);

            // Force mortality
            FusionPlayer.SetMortality(metadata.Config.Mortality);

            if (metadata.Config.Vitality_Enabled) FusionPlayer.SetPlayerVitality(metadata.Config.Vitality);
        }

        internal void SetStats()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() == null)
            {
                ClearOverrides();
            }
            else
            {
                var metadata = TeamManager.GetLocalTeam() == Survivors
                    ? SurvivorsMetadata : TeamManager.GetLocalTeam() == InfectedChildren
                    ? InfectedChildrenMetadata : InfectedMetadata;

                Internal_SetStats(metadata);
            }
        }

        private void SelectedPlayerOverride()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() != Infected && TeamManager.GetLocalTeam() != InfectedChildren)
                return;

            SwapAvatar(SelectedAvatar.ClientValue);
        }

        private void ApplyGamemodeSettings()
        {
            if (SelectMode == AvatarSelectMode.Random)
            {
                var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
                avatars.RemoveAll((Il2CppSystem.Predicate<AvatarCrate>)(x => x.Redacted));
                SelectedAvatar.ClientValue = avatars[UnityEngine.Random.RandomRangeInt(0, avatars.Count)].Barcode.ID;
            }

            CountdownValue.SetValue(CountdownLength);

            InfectedMetadata.ApplyConfig();
            InfectedChildrenMetadata.ApplyConfig();
            SurvivorsMetadata.ApplyConfig();

            AKI.SetValue(AllowKeepInventory);

            var now = DateTimeOffset.Now;
            lastTimeLimit = TimeLimit;
            StartUnix.SetValue(now.ToUnixTimeMilliseconds());
            if (!NoTimeLimit)
                EndUnix.SetValue(now.AddMinutes(TimeLimit).ToUnixTimeMilliseconds());
            else
                EndUnix.SetValue(-1);

            InfectedLooking.SetValue(false);
        }

        bool assignedTeams = false;

        public override void OnGamemodeStarted()
        {
            base.OnGamemodeStarted();

            PlayList.SetPlaylist(AudioReference.CreateReferences(Defaults.Tracks));
            PlayList.Shuffle();

            HasBeenInfected = false;
            _elapsedTime = 0f;
            _lastCheckedMinutes = 0;
            _oneMinuteLeft = false;
            assignedTeams = false;
            HideVision = false;
            InitialTeam = true;

            if (NetworkInfo.IsServer)
            {
                ApplyGamemodeSettings();
                AssignTeams();
                MelonCoroutines.Start(InfectedLookingWait());

                if (TeleportOnStart)
                    Teleport.TryInvoke();
            }

            PlayList.StartPlaylist();

            // Invoke player changes on level load
            FusionSceneManager.HookOnTargetLevelLoad(() =>
            {
                PopulatePage();
                if (!NetworkInfo.IsServer)
                {
                    InfectedMetadata.RefreshConfig(false);
                    SurvivorsMetadata.RefreshConfig(false);
                }
                SetStats();

                SelectedPlayerOverride();

                if (UseDeathmatchSpawns.ClientValue)
                    UseDeathmatchSpawns_Init(!TeleportOnStart);
                else
                    ClearDeathmatchSpawns();
            });
        }

        static bool appliedDeathmatchSpawns = false;

        private static void UseDeathmatchSpawns_Init(bool teleport = true)
        {
            if (appliedDeathmatchSpawns)
                return;

            appliedDeathmatchSpawns = true;

            var transforms = new List<Transform>();
            var markers = GamemodeMarker.FilterMarkers(null);

            foreach (var marker in markers)
            {
                transforms.Add(marker.transform);
            }

            FusionPlayer.SetSpawnPoints([.. transforms]);

            // Teleport to a random spawn point
            if (FusionPlayer.TryGetSpawnPoint(out var spawn) && teleport)
                FusionPlayer.Teleport(spawn.position, spawn.forward);
        }

        private static void ClearDeathmatchSpawns()
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
            PopulatePage();

            HasBeenInfected = false;
            InitialTeam = true;

            PlayList.StopPlaylist();

            if (NetworkInfo.IsServer)
            {
                TeamManager.UnassignAllPlayers();
            }
            else
            {
                if (TeleportOnEnd.ClientValue)
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
            var rig = Player.RigManager;
            if (rig != null)
            {
                var PCDs = rig?.gameObject?.GetComponentsInChildren<PullCordDevice>();
                PCDs?.ForEach(x => x._bodyLogEnabled = enabled);
            }
        }

        private static void TeleportToHost()
        {
            if (NetworkInfo.IsServer)
                return;

            var host = PlayerIdManager.GetHostId();

            if (!NetworkPlayerManager.TryGetPlayer(host, out var player))
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
            for (int i = 0; i < InfectedCount; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                TeamManager.TryAssignTeam(player, Infected);

                if (SelectMode == AvatarSelectMode.FirstInfected && string.IsNullOrWhiteSpace(selected))
                {
                    if (NetworkPlayerManager.TryGetPlayer(player.SmallId, out NetworkPlayer plr) && plr.HasRig)
                    {
                        var avatar = plr.RigRefs?.RigManager?.AvatarCrate?.Barcode?.ID;
                        if (!string.IsNullOrWhiteSpace(avatar))
                        {
                            selected = avatar;
                            SelectedAvatar.ClientValue = avatar;
                        }
                    }
                }

                players.Remove(player);
            }

            foreach (var plr in players)
                TeamManager.TryAssignTeam(plr, Survivors);
            assignedTeams = true;
        }

        private readonly Dictionary<PlayerId, PlayerActionType> lastActions = [];

        protected void OnPlayerAction(PlayerId player, PlayerActionType type, PlayerId otherPlayer = null)
        {
            if (!IsStarted)
                return;

            if (type == PlayerActionType.DYING_BY_OTHER_PLAYER)
            {
                if (!NetworkInfo.IsServer || otherPlayer == null || InfectType != InfectTypeEnum.DEATH)
                    return;

                var playerTeam = TeamManager.GetPlayerTeam(player);
                var otherPlayerTeam = TeamManager.GetPlayerTeam(otherPlayer);

                if (playerTeam == Survivors &&
                    (otherPlayerTeam == Infected || otherPlayerTeam == InfectedChildren))
                {
                    InfectEvent.TryInvoke(player.LongId.ToString());
                }
            }
            else if (type == PlayerActionType.DYING)
            {
                if (!NetworkInfo.IsServer || !SuicideInfects || otherPlayer != null)
                    return;

                if (lastActions.ContainsKey(player) && lastActions[player] == PlayerActionType.DYING_BY_OTHER_PLAYER)
                    return;

                if (TeamManager.GetPlayerTeam(player) == Survivors)
                    InfectEvent.TryInvoke(player.LongId.ToString());
            }
            lastActions[player] = type;
        }

        public enum InfectTypeEnum
        {
            TOUCH,
            DEATH
        }
    }
}