// Ignore Spelling: Metadata

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
using LabFusion.Menu.Gamemodes;
using LabFusion.Scene;
using LabFusion.Senders;
using LabFusion.Marrow.Proxies;
using LabFusion.SDK.Metadata;

using UnityEngine;

using MelonLoader;

using BoneLib;

using HarmonyLib;

using AvatarInfection.Utilities;
using System;
using System.Text.RegularExpressions;

namespace AvatarInfection
{
    public class Infection : Gamemode
    {
        public override string Title => "Avatar Infection";

        public override string Author => "HAHOOS";

        public override string Barcode => Defaults.Barcode;

        public override Texture Logo => Core.Icon;

        public override bool DisableSpawnGun => _DisableSpawnGun;

        public override bool DisableDevTools => _DisableDevTools;

        public override bool AutoHolsterOnDeath => true;

        public override bool DisableManualUnragdoll => true;

        public override bool AutoStopOnSceneLoad => true;

        public override bool ManualReady => false;

        internal MetadataBool __DisableSpawnGun;

        private static bool _DisableSpawnGun = Defaults.DisableSpawnGun;

        internal MetadataBool __DisableDevTools;

        private static bool _DisableDevTools = Defaults.DisableDevTools;

        public static Infection Instance { get; private set; }

        internal static Team Infected { get; } = new("Infected");

        internal static TeamMetadata InfectedMetadata;

        internal static Team UnInfected { get; } = new("Uninfected");

        internal static TeamMetadata UnInfectedMetadata;

        internal static TeamManager TeamManager { get; } = new();

        private const string TeamConfigName = "{0} Config";

        internal static class Defaults
        {
            public const string Barcode = "HAHOOS.AvatarInfection.Gamemode.AvatarInfection";

            public const int TimeLimit = 10;

            public const int InfectedBitReward = 50;

            public const int UnInfectedBitReward = 75;

            public const bool UntilAllFound = false;

            public readonly static TeamConfig InfectedStats = new()
            {
                Vitality = 0.5f,
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

            public readonly static TeamConfig UnInfectedStats = new()
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

        internal static MetadataVariableT<string> _SelectedAvatar;
        internal static string SelectedAvatar;
        public MusicPlaylist Playlist { get; } = new();

        private bool UntilAllFound { get; set; } = Defaults.UntilAllFound;

        internal TriggerEvent InfectEvent;
        internal TriggerEvent OneMinuteLeftEvent;
        internal TriggerEvent InfectedVictoryEvent;
        internal TriggerEvent UninfectedVictoryEvent;
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

        internal static bool ShouldTeleportToHost { get; private set; } = Defaults.ShouldTeleportToHost;

        internal static InfectTypeEnum InfectType { get; private set; } = Defaults.InfectType;

        internal MetadataBool AKI { get; private set; }

        internal static MetadataInt _CountdownLength;

        internal static int CountdownLength { get; private set; } = Defaults.CountdownLength;

        internal bool AllowKeepInventory { get; private set; } = Defaults.AllowKeepInventory;

        internal bool SuicideInfects { get; private set; } = Defaults.SuicideInfects;

        internal int HoldTime { get; private set; } = Defaults.HoldTime;

        internal MetadataBool _TeleportOnEnd { get; private set; }

        internal bool TeleportOnEnd { get; private set; } = Defaults.TeleportOnEnd;

        private List<ElementData> InfectedElements;
        private List<ElementData> UnInfectedElements;

        private static MenuElement GetElement(string groupName, string elementName, bool startsWith = true)
        {
            if (MenuGamemode.SelectedGamemode != Infection.Instance)
                return null;

            var group = MenuGamemode.SettingsPageElement.Elements.FirstOrDefault(x => x.Title == groupName) as GroupElement;
            if (group == null)
            {
                Logger.Warn("Cannot find group");
                return null;
            }

            var element = group.Elements?.FirstOrDefault(x => startsWith ? x.Title.StartsWith(elementName) : x.Title.Contains(elementName));
            if (element == null)
            {
                Logger.Warn("Cannot find element");
                return null;
            }

            return element;
        }

        private static void ChangeElementTitle(string groupName, string elementTitle, string newTitle, bool startsWith = true)
        {
            var element = GetElement(groupName, elementTitle, startsWith);
            if (element != null)
                element.Title = newTitle;
        }

        private static void ChangeElementIncrement(string groupName, string elementTitle, float increment, bool startsWith = true)
        {
            var element = GetElement(groupName, elementTitle, startsWith);
            if (element != null && element.GetType() == typeof(FloatElement))
                (element as FloatElement).Increment = increment;
        }

        private static void ChangeElementColor(string groupName, string elementTitle, System.Drawing.Color? color, bool startsWith = true)
        {
            if (MenuGamemode.SelectedGamemode != Infection.Instance)
                return;

            var group = MenuGamemode.SettingsPageElement.Elements.FirstOrDefault(x => x.Title == groupName) as GroupElement;
            if (group == null)
            {
                Logger.Warn("Cannot find group");
                return;
            }

            bool found = false;

            foreach (var element in group.Elements)
            {
                var title = RemoveColor(element.Title);
                if (!(startsWith ? title.StartsWith(elementTitle) : title.Contains(elementTitle)))
                    continue;

                found = true;

                if (color.HasValue)
                    element.Title = $"<color=#{color.Value.R:X2}{color.Value.G:X2}{color.Value.B:X2}>{title}</color>";
                else
                    element.Title = title;
            }
            if (!found)
                Logger.Warn("Cannot find element");
        }

        private static string RemoveColor(string text)
        {
            static string match(Match match)
            {
                if (match.Success)
                {
                    if (match.Groups.Count > 2)
                        return match.Groups[2].Value;
                }
                return string.Empty;
            }
            return Regex.Replace(text, @"<color=#(.*?)>(.*?)<\/color>", match);
        }

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

            GroupElementData avatarGroup = new("Avatar");

            group.AddElement(avatarGroup);

            FunctionElementData select = new()
            {
                Title = $"Selected Avatar: {(!string.IsNullOrWhiteSpace(SelectedAvatar) && SelectedAvatar != Il2CppSLZ.Marrow.Warehouse.Barcode.EMPTY ? new AvatarCrateReference(SelectedAvatar)?.Scannable?.Title ?? "N/A" : "N/A")}",
                OnPressed = () =>
                {
                    try
                    {
                        if (IsStarted)
                            return;

                        var rigManager = Player.RigManager;
                        if (rigManager?.AvatarCrate?.Barcode != null)
                        {
                            var avatar = rigManager.AvatarCrate.Barcode.ID;

                            if (string.IsNullOrWhiteSpace(avatar))
                            {
                                FusionNotifier.Send(new FusionNotification()
                                {
                                    Title = "Failure",
                                    Message = "Could not retrieve your current avatar :(",
                                    PopupLength = 2.5f,
                                    ShowPopup = true,
                                    Type = NotificationType.ERROR,
                                    SaveToMenu = false
                                });
                            }
                            SelectedAvatar = avatar;

                            if (IsStarted)
                                _SelectedAvatar.SetValue<string>(SelectedAvatar);

                            string title = !string.IsNullOrWhiteSpace(rigManager.AvatarCrate?.Scannable?.Title) ? rigManager.AvatarCrate.Scannable.Title : "N/A";
                            if (MenuGamemode.SelectedGamemode == this)
                            {
                                // HACK: This should not exist, but it does because you only send data for the elements
                                ChangeElementTitle(avatarGroup.Title, "Selected Avatar:", $"Selected Avatar: {title}");
                            }
                            if (new Barcode(rigManager.AvatarCrate.Barcode.ID)?.IsValid() == true)
                            {
                                FusionNotifier.Send(new FusionNotification()
                                {
                                    Title = "Success",
                                    Message = $"Successfully selected avatar '{title}' as the infected one!",
                                    PopupLength = 2.5f,
                                    Type = NotificationType.SUCCESS,
                                    SaveToMenu = false,
                                    ShowPopup = true
                                });
                            }
                            else
                            {
                                FusionNotifier.Send(new FusionNotification()
                                {
                                    Title = "Failure",
                                    Message = "Failed to select a new infected avatar :(",
                                    PopupLength = 2.5f,
                                    Type = NotificationType.ERROR,
                                    SaveToMenu = false,
                                    ShowPopup = true
                                });
                            }
                        }
                        else
                        {
                            FusionNotifier.Send(new FusionNotification()
                            {
                                Title = "Failure",
                                Message = "Failed to select a new infected avatar :( RigManager for local player was not found!",
                                PopupLength = 2.5f,
                                Type = NotificationType.ERROR,
                                SaveToMenu = false,
                                ShowPopup = true
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException("Select new Avatar", ex);
                        FusionNotifier.Send(new FusionNotification()
                        {
                            Title = "Failure",
                            Message = "An unexpected error has occured while trying to select a new avatar. Check console or logs for more information",
                            PopupLength = 3f,
                            Type = NotificationType.ERROR,
                            SaveToMenu = false,
                        });
                    }
                }
            };
            avatarGroup.AddElement(select);

            GroupElementData gr = new(string.Format(TeamConfigName, Infected.DisplayName));
            group.AddElement(gr);

            InfectedElements ??= GetElementsForTeam(true);
            InfectedElements.ForEach(x => gr.AddElement(x));

            var gr2 = new GroupElementData(string.Format(TeamConfigName, UnInfected.DisplayName));
            group.AddElement(gr2);

            UnInfectedElements ??= GetElementsForTeam(false);
            UnInfectedElements.ForEach(x => gr2.AddElement(x));

            GroupElementData generalGroup = new("General");

            group.AddElement(generalGroup);

            var infectedCount = new IntElementData()
            {
                Title = "Infected Start Number",
                Increment = 1,
                MinValue = 1,
                MaxValue = 5,
                Value = InfectedCount,
                OnValueChanged = (val) => InfectedCount = val,
            };
            generalGroup.AddElement(infectedCount);

            var timeLimit = new IntElementData()
            {
                Title = "Time Limit",
                Value = TimeLimit,
                Increment = 1,
                MinValue = 1,
                MaxValue = 60,
                OnValueChanged = (val) => TimeLimit = val
            };
            generalGroup.AddElement(timeLimit);

            var spawnGunDisabled = new BoolElementData()
            {
                Title = "Disable Spawn Gun",
                Value = DisableSpawnGun,
                OnValueChanged = (val) =>
                {
                    _DisableSpawnGun = val;
                    __DisableSpawnGun.SetValue(val);
                }
            };
            generalGroup.AddElement(spawnGunDisabled);

            var devToolsDisabled = new BoolElementData()
            {
                Title = "Disable Dev Tools",
                Value = DisableDevTools,
                OnValueChanged = (val) =>
                {
                    _DisableDevTools = val;
                    __DisableDevTools.SetValue(val);
                }
            };
            generalGroup.AddElement(devToolsDisabled);

            var allowKI = new BoolElementData()
            {
                Title = "Allow Keep Inventory",
                Value = AllowKeepInventory,
                OnValueChanged = (val) =>
                {
                    AllowKeepInventory = val;
                    if (IsStarted)
                        AKI.SetValue(val);
                }
            };
            generalGroup.AddElement(allowKI);

            var untilAllFound = new BoolElementData()
            {
                Title = "Play Until All Found (No Time Limit)",
                Value = UntilAllFound,
                OnValueChanged = (val) => UntilAllFound = val
            };
            generalGroup.AddElement(untilAllFound);

            var teleportToHostOnStart = new BoolElementData()
            {
                Title = "Teleport To Host On Start",
                Value = ShouldTeleportToHost,
                OnValueChanged = (val) => ShouldTeleportToHost = val,
            };
            generalGroup.AddElement(teleportToHostOnStart);

            var teleportToHostOnEnd = new BoolElementData()
            {
                Title = "Teleport To Host On End",
                Value = TeleportOnEnd,
                OnValueChanged = (val) =>
                {
                    TeleportOnEnd = val;
                    if (IsStarted)
                        _TeleportOnEnd.SetValue(val);
                }
            };
            generalGroup.AddElement(teleportToHostOnEnd);

            var countdownLength = new IntElementData()
            {
                Title = "Countdown Length",
                Increment = 5,
                MaxValue = 3600,
                MinValue = 0,
                Value = CountdownLength,
                OnValueChanged = (val) =>
                {
                    CountdownLength = val;
                    if (IsStarted)
                        _CountdownLength.SetValue(val);
                }
            };
            generalGroup.AddElement(countdownLength);

            var infectType = new FunctionElementData()
            {
                Title = $"Infect Type: {InfectType}",
                OnPressed = () =>
                {
                    var values = Enum.GetValues(typeof(InfectTypeEnum));
                    int index = (int)InfectType;

                    // Politely borrowed from BoneLib
                    // Oh sorry my bad, from LabFusion, because the BoneLib one didn't do anything
                    index++;
                    index %= values.Length;
                    InfectType = (InfectTypeEnum)values.GetValue(index);
                    ChangeElementTitle(generalGroup.Title, "Infect Type:", $"Infect Type: {InfectType}");
                }
            };

            // I didn't think what I was doing and instead of doing "Suicide Infects" i did "Suicide Kills"
            // Yeah it fucking kills dumbass
            // I'm keeping 'suicideKills' as the variable name to remind myself how smart I am
            var suicideKills = new BoolElementData()
            {
                Title = "Suicide Infects (Death Infect Type)",
                Value = true,
                OnValueChanged = (val) => SuicideInfects = val,
            };
            generalGroup.AddElement(suicideKills);

            var holdTime = new IntElementData()
            {
                Title = "Hold Time (Touch Infect Type)",
                Value = HoldTime,
                Increment = 1,
                MaxValue = 60,
                MinValue = 0,
                OnValueChanged = (val) => HoldTime = val,
            };
            generalGroup.AddElement(holdTime);

            /*
            var infectType = new EnumElementData()
            {
                Title = "Infect Type",
                EnumType = typeof(InfectTypeEnum),
                Value = InfectType,
                OnValueChanged = (val) =>
                {
                    InfectType = (InfectTypeEnum)val;
                    if (IsStarted || GamemodeManager.ActiveGamemode == this)
                        _InfectType.SetValue(InfectType);
                }
            };
            */
            generalGroup.AddElement(infectType);

            var resetToDefault = new FunctionElementData()
            {
                Title = "Reset To Default Settings",
                OnPressed = () =>
                {
                    if (IsStarted)
                        return;

                    InfectedMetadata.Config = new TeamConfig(Defaults.InfectedStats);
                    UnInfectedMetadata.Config = new TeamConfig(Defaults.UnInfectedStats);
                    UntilAllFound = Defaults.UntilAllFound;
                    SelectedAvatar = string.Empty;
                    TimeLimit = Defaults.TimeLimit;
                    InfectType = Defaults.InfectType;
                    SuicideInfects = Defaults.SuicideInfects;
                    HoldTime = Defaults.HoldTime;
                    InfectedCount = Defaults.InfectedCount;
                    CountdownLength = Defaults.CountdownLength;
                    _DisableSpawnGun = Defaults.DisableSpawnGun;
                    _DisableDevTools = Defaults.DisableDevTools;
                    AllowKeepInventory = Defaults.AllowKeepInventory;
                    ShouldTeleportToHost = Defaults.ShouldTeleportToHost;
                    TeleportOnEnd = Defaults.TeleportOnEnd;

                    ChangeElementTitle("Avatar", "Selected Avatar:", "Selected Avatar: N/A");
                    ChangeElementTitle("General", "Infect Type:", $"Infect Type: {Enum.GetName(InfectType)}");
                }
            };
            generalGroup.AddElement(resetToDefault);

#if DEBUG

            var debugGroup = new GroupElementData()
            {
                Title = "Debug"
            };
            group.AddElement(debugGroup);

            var label = new FunctionElementData()
            {
                Title = "Currently has nothing",
                OnPressed = () => Logger.Log("Nothing")
            };
            group.AddElement(label);
#endif

            return group;
        }

        private List<ElementData> GetElementsForTeam(bool InfectedTeam)
        {
            var metadata = InfectedTeam ? InfectedMetadata : UnInfectedMetadata;
            var group = new List<ElementData>();

            var apply = new FunctionElementData()
            {
                Title = "Apply new settings (use when the gamemode is already started)",
                OnPressed = () =>
                {
                    if (IsStarted)
                    {
                        var remote = metadata.GetConfigFromMetadata();
                        if (remote == metadata.Config)
                            return;

                        metadata.ApplyConfig();
                        RefreshStatsEvent?.TryInvoke(InfectedTeam.ToString());
                    }
                }
            };
            group.Add(apply);

            var mortality = new BoolElementData()
            {
                Title = "Mortality",
                Value = metadata.Config.Mortality,
                OnValueChanged = (val) => metadata.Config.Mortality = val
            };
            group.Add(mortality);

            var canUseGuns = new BoolElementData()
            {
                Title = "Can Use Guns",
                Value = metadata.Config.CanUseGuns,
                OnValueChanged = (val) => metadata.Config.CanUseGuns = val
            };
            group.Add(canUseGuns);

            var overrideVitality = new BoolElementData()
            {
                Title = "Override Vitality",
                Value = metadata.Config.Vitality_Enabled,
                OnValueChanged = (val) => metadata.Config.Vitality_Enabled = val
            };

            group.Add(overrideVitality);

            var vitality = new FloatElementData()
            {
                Title = "Vitality",
                Value = metadata.Config.Vitality,
                Increment = Increment,
                MinValue = 0.2f,
                MaxValue = 100f,
                OnValueChanged = (v) => metadata.Config.Vitality = v
            };
            group.Add(vitality);

            var overrideSpeed = new BoolElementData()
            {
                Title = "Override Speed",
                Value = metadata.Config.Speed_Enabled,
                OnValueChanged = (val) => metadata.Config.Speed_Enabled = val
            };

            group.Add(overrideSpeed);

            var speed = new FloatElementData()
            {
                Title = "Speed",
                Value = metadata.Config.Speed,
                Increment = Increment,
                MinValue = 0.2f,
                MaxValue = 100f,
                OnValueChanged = (v) => metadata.Config.Speed = v
            };
            group.Add(speed);

            var overrideJumpPower = new BoolElementData()
            {
                Title = "Override Jump Power",
                Value = metadata.Config.JumpPower_Enabled,
                OnValueChanged = (val) => metadata.Config.JumpPower_Enabled = val
            };

            group.Add(overrideJumpPower);

            var jumpPower = new FloatElementData()
            {
                Title = "Jump Power",
                Value = metadata.Config.JumpPower,
                Increment = Increment,
                MinValue = 0.2f,
                MaxValue = 100f,
                OnValueChanged = (v) => metadata.Config.JumpPower = v
            };
            group.Add(jumpPower);

            var overrideAgility = new BoolElementData()
            {
                Title = "Override Agility",
                Value = metadata.Config.Agility_Enabled,
                OnValueChanged = (val) => metadata.Config.Agility_Enabled = val
            };

            group.Add(overrideAgility);

            var agility = new FloatElementData()
            {
                Title = "Agility",
                Value = metadata.Config.Agility,
                Increment = Increment,
                MinValue = 0.2f,
                MaxValue = 100f,
                OnValueChanged = (v) => metadata.Config.Agility = v
            };
            group.Add(agility);

            var overrideStrengthUpper = new BoolElementData()
            {
                Title = "Override Strength Upper",
                Value = metadata.Config.StrengthUpper_Enabled,
                OnValueChanged = (val) => metadata.Config.StrengthUpper_Enabled = val
            };

            group.Add(overrideStrengthUpper);

            var strengthUpper = new FloatElementData()
            {
                Title = "Strength Upper",
                Value = metadata.Config.StrengthUpper,
                Increment = Increment,
                MinValue = 0.2f,
                MaxValue = 100f,
                OnValueChanged = (v) => metadata.Config.StrengthUpper = v
            };

            group.Add(strengthUpper);

            // Borrowed from Infect Type Enum element thingy which initially borrowed from BoneLib which then borrowed from LabFusion
            var increment = new FunctionElementData()
            {
                Title = $"Increment: {Increment}",
                OnPressed = () =>
                {
                    var group = string.Format(TeamConfigName, metadata.Team.DisplayName);

                    IncrementIndex++;
                    IncrementIndex %= IncrementValues.Count;
                    ChangeElementTitle(group, "Increment:", $"Increment: {Increment}");

                    ChangeElementIncrement(group, "Strength Upper", Increment);
                    ChangeElementIncrement(group, "Agility", Increment);
                    ChangeElementIncrement(group, "Jump Power", Increment);
                    ChangeElementIncrement(group, "Speed", Increment);
                    ChangeElementIncrement(group, "Vitality", Increment);
                }
            };
            group.Add(increment);

            return group;
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

        public override void OnGamemodeRegistered()
        {
            Instance = this;
            FusionOverrides.OnValidateNametag += OnValidateNametag;
            MultiplayerHooking.OnPlayerAction += OnPlayerAction;

            TeamManager.Register(this);
            TeamManager.AddTeam(Infected);
            TeamManager.AddTeam(UnInfected);
            TeamManager.OnAssignedToTeam += OnAssignedToTeam;

            __DisableDevTools = new MetadataBool(nameof(DisableDevTools), Metadata);
            __DisableSpawnGun = new MetadataBool(nameof(DisableSpawnGun), Metadata);

            InfectedMetadata = new TeamMetadata(Infected, Metadata, new TeamConfig(Defaults.InfectedStats));
            UnInfectedMetadata = new TeamMetadata(UnInfected, Metadata, new TeamConfig(Defaults.UnInfectedStats));

            _SelectedAvatar = new MetadataVariableT<string>(nameof(SelectedAvatar), Metadata);

            _CountdownLength = new MetadataInt(nameof(CountdownLength), Metadata);

            AKI = new MetadataBool("AllowKeepInventory", Metadata);

            InfectedLooking = new MetadataBool(nameof(InfectedLooking), Metadata);

            _TeleportOnEnd = new MetadataBool(nameof(TeleportOnEnd), Metadata);

            Metadata.OnMetadataChanged += OnMetadataChanged;

            InfectEvent = new TriggerEvent("InfectPlayer", Relay, true);
            InfectEvent.OnTriggeredWithValue += PlayerInfected;

            OneMinuteLeftEvent = new TriggerEvent("OneMinuteLeft", Relay, true);
            OneMinuteLeftEvent.OnTriggered += OneMinuteLeft;

            InfectedVictoryEvent = new TriggerEvent("InfectedVictory", Relay, true);
            InfectedVictoryEvent.OnTriggered += InfectedVictory;

            UninfectedVictoryEvent = new TriggerEvent("UninfectedVictory", Relay, true);
            UninfectedVictoryEvent.OnTriggered += UnInfectedVictory;

            RefreshStatsEvent = new TriggerEvent("RefreshEvent", Relay, true);
            RefreshStatsEvent.OnTriggeredWithValue += RefreshStats;

            Teleport = new TriggerEvent("TeleportToHost", Relay, true);
            Teleport.OnTriggered += TeleportToHost;
        }

        private void RefreshStats(string isInfectedTeam)
        {
            bool success = bool.TryParse(isInfectedTeam, out bool infected);
            if (!success)
                return;

            if (TeamManager.GetLocalTeam() == (infected ? Infected : UnInfected))
                SetStats();
        }

        private void InfectedLookingEvent()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() != Infected)
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

        public override void OnGamemodeUnregistered()
        {
            FusionOverrides.OnValidateNametag -= OnValidateNametag;

            InfectedElements = null;
            UnInfectedElements = null;

            TeamManager.Unregister();

            Metadata.OnMetadataChanged -= OnMetadataChanged;

            InfectEvent?.UnregisterEvent();
            InfectEvent = null;

            OneMinuteLeftEvent?.UnregisterEvent();
            OneMinuteLeftEvent = null;

            InfectedVictoryEvent?.UnregisterEvent();
            InfectedVictoryEvent = null;

            UninfectedVictoryEvent?.UnregisterEvent();
            UninfectedVictoryEvent = null;

            RefreshStatsEvent?.UnregisterEvent();
            RefreshStatsEvent = null;

            Teleport?.UnregisterEvent();
            Teleport = null;
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

            if (NetworkInfo.IsServer && UnInfected.HasPlayer(playerId))
            {
                if (UnInfected.PlayerCount <= 1)
                {
                    InfectedVictoryEvent.TryInvoke();
                    GamemodeManager.StopGamemode();
                }
                else
                {
                    TeamManager.TryAssignTeam(playerId, Infected);
                }
            }

            if (playerId.IsMe && !HasBeenInfected)
            {
                if (UnInfected.PlayerCount > 1)
                    SwapAvatar(SelectedAvatar);

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
                    Message = $"{(string.IsNullOrWhiteSpace(displayName) ? "N/A" : displayName)} is now infected, {(UnInfected.PlayerCount > 1 ? "look out for them..." : "the last survivor has fallen...")}",
                    PopupLength = 4,
                    Type = NotificationType.INFORMATION
                });
            }
        }

        private static void SwapAvatar(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode == Il2CppSLZ.Marrow.Warehouse.Barcode.EMPTY)
            {
                Logger.Error("ALERT! ALERT! This is not supposed to fucking happen, what the fuck did you do that the SelectedAvatar is empty. Now relax, calm down and fix this issue");
                return;
            }

            if (Player.RigManager == null)
                return;

            var obj = new GameObject("AI_PCFC");
            var comp = obj.AddComponent<PullCordForceChange>();
            comp.avatarCrate = new AvatarCrateReference(barcode);
            comp.rigManager = Player.RigManager;
            PullCordSender.SendBodyLogEffect();
            comp.ForceChange(comp.rigManager.gameObject);
        }

        private IEnumerator InfectedLookingWait()
        {
            int target = CountdownLength;

            float passed = 0f;

            while (passed < target)
            {
                passed += TimeUtilities.DeltaTime;
                yield return null;
            }

            InfectedLooking.SetValue(true);
        }

        private void OnAssignedToTeam(PlayerId player, Team team)
        {
            FusionOverrides.ForceUpdateOverrides();

            if (team == null || player?.IsValid != true)
                return;

            if (!player.IsMe)
                return;

            SetStats();
            var config = team == Infected ? InfectedMetadata : UnInfectedMetadata;
            config.CanUseGunsChanged();
            if (team == Infected)
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

            if (team == Infected)
            {
                SwapAvatar(SelectedAvatar);
                MelonCoroutines.Start(HideVisionAndReveal());
            }
            else
            {
                FusionNotifier.Send(new FusionNotification()
                {
                    ShowPopup = true,
                    Title = "Uninfected",
                    Message = "Woah! You were lucky to not be infected. You have to make sure you don't get infected!",
                    PopupLength = 4,
                    Type = NotificationType.INFORMATION,
                });
            }
            InitialTeam = false;
        }

        private void OneMinuteLeft()
        {
            if (!IsStarted)
                return;

            FusionNotifier.Send(new FusionNotification()
            {
                Title = "Avatar Infection",
                Message = "One minute left!",
                PopupLength = 3.5f,
                ShowPopup = true,
                Type = NotificationType.INFORMATION,
            });
        }

        private void InfectedVictory()
        {
            if (!IsStarted)
                return;

            FusionNotifier.Send(new FusionNotification()
            {
                ShowPopup = true,
                Title = "Infected Won",
                Message = "Everyone has been infected!",
                PopupLength = 4,
                Type = NotificationType.INFORMATION,
            });
        }

        private void UnInfectedVictory()
        {
            if (!IsStarted)
                return;

            FusionNotifier.Send(new FusionNotification()
            {
                ShowPopup = true,
                Title = "UnInfected Won",
                Message = "There were people not infected in time!",
                PopupLength = 4,
                Type = NotificationType.INFORMATION,
            });
        }

        private IEnumerator HideVisionAndReveal()
        {
            if (IsStarted)
            {
#if DEBUG
                const bool skip = false;
#else
                const bool skip = false;
#endif
                try
                {
                    CountdownLength = _CountdownLength.GetValue();
                    if (!skip && CountdownLength != 0)
                    {
                        HideVision = true;

                        LocalVision.Blind = true;
                        LocalVision.BlindColor = Color.black;

                        //Countdown.IsHidden = false;
                        //Countdown.Opacity = 1;

                        // Lock movement so we can't move while vision is dark
                        LocalControls.LockMovement();

                        const float fadeLength = 1f;

                        float elapsed = 0f;
                        float totalElapsed = 0f;

                        int seconds = 0;

                        bool secondPassed = true;

                        while (seconds < CountdownLength)
                        {
                            if (!IsStarted)
                                break;

                            // Calculate fade-in
                            float fadeStart = Mathf.Max(CountdownLength - fadeLength, 0f);
                            float fadeProgress = Mathf.Max(totalElapsed - fadeStart, 0f) / fadeLength;

                            Color color = Color.Lerp(Color.black, Color.clear, fadeProgress);

                            LocalVision.BlindColor = color;

                            // Check for second counter
                            if (secondPassed)
                            {
                                int remainingSeconds = CountdownLength - seconds;

                                var icon = Texture2D.whiteTexture;
                                var sprite = Sprite.Create(icon, new Rect(0, 0, 0, 0), new Vector2(0.5f, 0.5f), 100f);

                                // Make sure tutorial rig and head titles are enabled

                                var tutorialRig = TutorialRig.Instance;
                                var headTitles = tutorialRig.headTitles;

                                tutorialRig.gameObject.SetActive(true);
                                headTitles.gameObject.SetActive(true);

                                float timeToScale = Mathf.Lerp(0.05f, 0.4f, Mathf.Clamp01(CountdownLength - 1f));

                                tutorialRig.headTitles.timeToScale = timeToScale;

                                tutorialRig.headTitles.CUSTOMDISPLAY(
                                    "Countdown, get ready...",
                                    remainingSeconds.ToString(),
                                    sprite,
                                    CountdownLength);
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

                        LocalControls.UnlockMovement();

                        TutorialRig.Instance.headTitles.CLOSEDISPLAY();

                        LocalVision.Blind = false;
                    }
                    FusionNotifier.Send(new FusionNotification()
                    {
                        ShowPopup = true,
                        Title = "Countdown Over",
                        Message = "GO AND INFECT THEM ALL!",
                        PopupLength = 2f,
                        Type = NotificationType.INFORMATION,
                    });
                }
                finally
                {
                    HideVision = false;
                }
            }
        }

        internal bool _oneMinuteLeft = false;

        protected override void OnUpdate()
        {
            if (!IsStarted)
            {
                ChangeElementColor(string.Format(TeamConfigName, Infected.DisplayName), "Apply new settings", null, false);
                ChangeElementColor(string.Format(TeamConfigName, UnInfected.DisplayName), "Apply new settings", null, false);

                return;
            }
            else
            {
                var remote = InfectedMetadata.GetConfigFromMetadata();
                if (remote != InfectedMetadata.Config)
                    ChangeElementColor(string.Format(TeamConfigName, Infected.DisplayName), "Apply new settings", System.Drawing.Color.Red, false);
                else
                    ChangeElementColor(string.Format(TeamConfigName, Infected.DisplayName), "Apply new settings", null, false);

                var remote2 = UnInfectedMetadata.GetConfigFromMetadata();
                if (remote2 != UnInfectedMetadata.Config)
                    ChangeElementColor(string.Format(TeamConfigName, UnInfected.DisplayName), "Apply new settings", System.Drawing.Color.Red, false);
                else
                    ChangeElementColor(string.Format(TeamConfigName, UnInfected.DisplayName), "Apply new settings", null, false);
            }

            if (HideVision && !LocalVision.Blind)
                LocalVision.Blind = true;

            _elapsedTime += TimeUtilities.DeltaTime;

            if (TeamManager.GetLocalTeam() == UnInfected)
                UninfectedUpdate();

            var rig = Player.RigManager;
            var PCDs = rig.gameObject.GetComponentsInChildren<PullCordDevice>();

            if (TeamManager.GetLocalTeam() == Infected)
                PCDs.ForEach(x => x._bodyLogEnabled = false);
            else
                PCDs.ForEach(x => x._bodyLogEnabled = true);

            if (!UntilAllFound)
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
                    UninfectedVictoryEvent?.TryInvoke();
                    GamemodeManager.StopGamemode();
                }
            }
        }

        private int _lastCheckedMinutes = 0;

        private void UninfectedUpdate()
        {
            if (_lastCheckedMinutes != ElapsedMinutes)
            {
                _lastCheckedMinutes = ElapsedMinutes;

                PointItemManager.RewardBits(Defaults.UnInfectedBitReward);
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
                var metadata = TeamManager.GetLocalTeam() == UnInfected ? UnInfectedMetadata : InfectedMetadata;

                Internal_SetStats(metadata);
            }
        }

        private void UpdateDevToolsSpawnGunBlacklist()
        {
            if (!NetworkInfo.IsServer)
            {
                _DisableDevTools = __DisableDevTools.GetValue();
                _DisableSpawnGun = __DisableSpawnGun.GetValue();
            }
        }

        private void SelectedPlayerOverride()
        {
            if (!IsStarted)
                return;

            if (TeamManager.GetLocalTeam() != Infected)
                return;

            SwapAvatar(SelectedAvatar);
        }

        private new void OnMetadataChanged(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            if (!IsStarted)
                return;

            switch (key)
            {
                case nameof(DisableDevTools):
                case nameof(DisableSpawnGun):
                    UpdateDevToolsSpawnGunBlacklist();
                    break;

                case nameof(SelectedAvatar):
                    if (SelectedAvatar == value)
                        return;
                    SelectedAvatar = _SelectedAvatar.GetValue();
                    SelectedPlayerOverride();
                    break;

                case nameof(CountdownLength):
                    CountdownLength = _CountdownLength.GetValue();
                    break;

                case nameof(InfectedLooking):
                    if (InfectedLooking.GetValue())
                        InfectedLookingEvent();
                    break;

                case nameof(TeleportOnEnd):
                    TeleportOnEnd = _TeleportOnEnd.GetValue();
                    break;
            }
        }

        private void ApplyGamemodeSettings()
        {
            _SelectedAvatar.SetValue(SelectedAvatar);

            __DisableDevTools.SetValue(_DisableDevTools);
            __DisableSpawnGun.SetValue(_DisableSpawnGun);

            _CountdownLength.SetValue(CountdownLength);

            _TeleportOnEnd.SetValue(TeleportOnEnd);

            InfectedMetadata.ApplyConfig();
            UnInfectedMetadata.ApplyConfig();

            AKI.SetValue(AllowKeepInventory);

            InfectedLooking.SetValue(false);
        }

        public override void OnGamemodeStarted()
        {
            base.OnGamemodeStarted();

            Playlist.SetPlaylist(AudioReference.CreateReferences(Defaults.Tracks));
            Playlist.Shuffle();

            HasBeenInfected = false;
            _elapsedTime = 0f;
            _lastCheckedMinutes = 0;
            _oneMinuteLeft = false;

            if (NetworkInfo.IsServer)
            {
                ApplyGamemodeSettings();
                AssignTeams();
                MelonCoroutines.Start(InfectedLookingWait());
                if (ShouldTeleportToHost)
                    Teleport.TryInvoke();
            }

            Playlist.StartPlaylist();

            // Invoke player changes on level load
            FusionSceneManager.HookOnTargetLevelLoad(() =>
            {
                if (!NetworkInfo.IsServer)
                {
                    InfectedMetadata.RefreshConfig(false);
                    UnInfectedMetadata.RefreshConfig(false);
                }
                SetStats();

                UpdateDevToolsSpawnGunBlacklist();

                if (!NetworkInfo.IsServer)
                    SelectedAvatar = _SelectedAvatar.GetValue();
                SelectedPlayerOverride();
            });
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

            HasBeenInfected = false;
            InitialTeam = true;

            Playlist.StopPlaylist();

            if (NetworkInfo.IsServer)
            {
                TeamManager.UnassignAllPlayers();
            }
            else
            {
                if (TeleportOnEnd)
                    TeleportToHost();
            }

            var rig = Player.RigManager;
            var PCDs = rig.gameObject.GetComponentsInChildren<PullCordDevice>();
            PCDs.ForEach(x => x._bodyLogEnabled = true);

            FusionOverrides.ForceUpdateOverrides();

            _elapsedTime = 0f;
            _lastCheckedMinutes = 0;
            _oneMinuteLeft = false;

            ClearOverrides();
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

        protected bool OnValidateNametag(PlayerId id)
        {
            if (!IsStarted)
                return true;

            return TeamManager.GetPlayerTeam(id) == null || TeamManager.GetPlayerTeam(id) == TeamManager.GetLocalTeam();
        }

        private void AssignTeams()
        {
            var players = new List<PlayerId>(PlayerIdManager.PlayerIds);
            players.Shuffle();

            var rand = new System.Random();
            for (int i = 0; i < InfectedCount; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                TeamManager.TryAssignTeam(player, Infected);

                players.Remove(player);
            }

            foreach (var plr in players)
                TeamManager.TryAssignTeam(plr, UnInfected);
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

                if (TeamManager.GetPlayerTeam(player) == UnInfected && TeamManager.GetPlayerTeam(otherPlayer) == Infected)
                    InfectEvent.TryInvoke(player.LongId.ToString());
            }
            else if (type == PlayerActionType.DYING)
            {
                if (!NetworkInfo.IsServer || InfectType != InfectTypeEnum.DEATH || !SuicideInfects)
                    return;

                if (lastActions.ContainsKey(player) && lastActions[player] == PlayerActionType.DYING_BY_OTHER_PLAYER)
                    return;

                if (TeamManager.GetPlayerTeam(player) == UnInfected)
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