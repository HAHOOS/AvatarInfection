using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using BoneLib;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Marrow.Proxies;
using LabFusion.Menu.Data;
using LabFusion.Menu.Gamemodes;

using static AvatarInfection.Infection;

namespace AvatarInfection.Managers
{
    internal static class GamemodeMenuManager
    {
        private const string TeamConfigName = "{0} Stats";

        private static readonly Dictionary<string, int> IncrementTeams = [];

        private static readonly IReadOnlyList<float> IncrementValues = [0.2f, 0.5f, 1f, 5f];

        private static string SelectedAvatarTitle;

        internal static GroupElementData CreateSettingsGroup()
        {
            // HACK: for some reason if i wouldn't have done this the settings wouldn't work
            var group = new GroupElementData()
            {
                Title = Instance.Title,
            };

            if (group == null)
            {
                FusionModule.Logger.Error("Group is null");
                return null;
            }

            GroupElementData avatarGroup = group.AddGroup("Avatar");

            avatarGroup.AddElement("Select Mode", Instance.Config.SelectMode.Value, (val) => { Instance.Config.SelectMode.Value = (AvatarSelectMode)val; RefreshSettingsPage(); });

            SelectedAvatarTitle = GetBarcodeTitle(Instance.Config.SelectedAvatar.ClientValue);
            if (Instance.Config.SelectMode.Value == AvatarSelectMode.CONFIG)
            {
                avatarGroup.AddElement(SelectedAvatarTitle, null);

                avatarGroup.AddElement("Select From Current Avatar", SelectNewAvatar);
            }

            group.AddElement(CreateElementsForTeam(Instance.Infected));

            if (Instance.Config.AddInfectedChildrenTeam.Value)
                group.AddElement(CreateElementsForTeam(Instance.InfectedChildren));

            group.AddElement(CreateElementsForTeam(Instance.Survivors));
            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", Instance.Config.InfectedCount.Value, (val) => Instance.Config.InfectedCount.Value = val, min: 1, max: 5);

            generalGroup.AddElement("No Time Limit", Instance.Config.NoTimeLimit.Value, (val) =>
            {
                Instance.Config.NoTimeLimit.Value = val;
                if (Instance.IsStarted)
                    Instance.EndUnix.SetValue(-1);
                RefreshSettingsPage();
            });

            if (!Instance.Config.NoTimeLimit.Value)
            {
                generalGroup.AddElement("Time Limit", Instance.Config.TimeLimit.Value, (val) =>
                {
                    Instance.Config.TimeLimit.Value = val;
                    if (Instance.IsStarted)
                        Instance.EndUnix.SetValue(DateTimeOffset.FromUnixTimeMilliseconds((long)Instance.StartUnix.GetValue()).AddMinutes(val).ToUnixTimeMilliseconds());
                }, min: 1);
            }

            generalGroup.AddElement("Disable Spawn Gun", Instance.DisableSpawnGun, (val) => Instance.Config.DisableSpawnGun.ClientValue = val);

            generalGroup.AddElement("Disable Developer Tools", Instance.DisableDevTools, (val) => Instance.Config.DisableDevTools.ClientValue = val);

            generalGroup.AddElement("Allow Keep Inventory", Instance.Config.AllowKeepInventory.ClientValue, (val) => Instance.Config.AllowKeepInventory.ClientValue = val);

            generalGroup.AddElement("Use DeathMatch Spawns", Instance.Config.UseDeathmatchSpawns.ClientValue, (val) => Instance.Config.UseDeathmatchSpawns.ClientValue = val);

            generalGroup.AddElement("Teleport To Host On Start", Instance.Config.TeleportOnStart.Value, (val) => Instance.Config.TeleportOnStart.Value = val);

            generalGroup.AddElement("Teleport To Host On End", Instance.Config.TeleportOnEnd.ClientValue, (val) => Instance.Config.TeleportOnEnd.ClientValue = val);

            generalGroup.AddElement("Countdown Length", Instance.Config.CountdownLength.ClientValue, (val) => Instance.Config.CountdownLength.ClientValue = val, 5, 0, 3600);
            generalGroup.AddElement("Show Countdown to All Players", Instance.Config.ShowCountdownToAll.ClientValue, (val) => Instance.Config.ShowCountdownToAll.ClientValue = val);

            generalGroup.AddElement("Infect Type", Instance.Config.InfectType.Value, (val) => { Instance.Config.InfectType.Value = (InfectType)val; RefreshSettingsPage(); });

            if (Instance.Config.InfectType.Value == InfectType.TOUCH)
                generalGroup.AddElement("Hold Time", Instance.Config.HoldTime.Value, (val) => Instance.Config.HoldTime.Value = val, max: 60);

            generalGroup.AddElement("Suicide Infects", Instance.Config.SuicideInfects.Value, (val) => Instance.Config.SuicideInfects.Value = val);
            generalGroup.AddElement("Save Settings", () =>
            {
                try
                {
                    Instance.TeamManager.InfectedTeams.ForEach(x => x.Metadata.Save(false));
                    Instance.Config.Save();
                    MenuHelper.ShowNotification("Success", "Successfully saved settings!", 3.5f);
                }
                catch (Exception ex)
                {
                    Core.Logger.Error(ex);
                    MenuHelper.ShowNotification("Fail", "Failed to save settings, check console for more details", 5f, type: LabFusion.UI.Popups.NotificationType.ERROR);
                }
            });
            generalGroup.AddElement("Load Settings", () =>
            {
                try
                {
                    Instance.Config.Load();
                    Instance.TeamManager.InfectedTeams.ForEach(x => x.Metadata.Load());
                    RefreshSettingsPage();
                    MenuHelper.ShowNotification("Success", "Successfully loaded settings!", 3.5f);
                }
                catch (Exception ex)
                {
                    Core.Logger.Error(ex);
                    MenuHelper.ShowNotification("Fail", "Failed to load settings, check console for more details", 5f, type: LabFusion.UI.Popups.NotificationType.ERROR);
                }
            });

            return group;
        }

        private static void SelectNewAvatar()
        {
            if (Instance.IsStarted)
                return;

            var rigManager = Player.RigManager;
            if (rigManager?.AvatarCrate?.Barcode != null)
            {
                var avatar = rigManager.AvatarCrate.Barcode.ID;

                if (string.IsNullOrWhiteSpace(avatar))
                    return;

                Instance.Config.SelectedAvatar.ClientValue = avatar;

                string title = !string.IsNullOrWhiteSpace(rigManager.AvatarCrate?.Scannable?.Title) ? rigManager.AvatarCrate.Scannable.Title : "N/A";

                Instance.ChangeElement<FunctionElement>("Avatar", SelectedAvatarTitle, (element) => element.Title = title, false);
                SelectedAvatarTitle = title;
            }
        }

        private static string GetBarcodeTitle(string barcode)
            => !string.IsNullOrWhiteSpace(barcode) ? (new AvatarCrateReference(barcode).Crate.Title ?? "N/A") : "N/A";

        internal static GroupElementData CreateElementsForTeam(InfectionTeam team)
        {
            var group = new GroupElementData()
            {
                Title = team.GetGroupName(),
            };

            if (Instance.IsStarted)
                group.AddElement(FormatApplyName(team, apply: false), () => ApplyMetadata(team));

            team.Metadata._settingsList.Types(x =>
            {
                if (x is ToggleServerSetting<float> toggleStat)
                    group.CreateStatElement(team, toggleStat);
                else if (x is ServerSetting<bool> boolStat)
                    group.CreateStatElement(team, boolStat);
            }, typeof(ToggleServerSetting<float>), typeof(ServerSetting<bool>));

            group.AddElement($"Increment: {GetIncrement(team.TeamName)}", () =>
            {
                if (!IncrementTeams.TryGetValue(team.TeamName, out int index))
                    index = 0;

                index++;
                index %= IncrementValues.Count;
                IncrementTeams[team.TeamName] = index;

                const string startsWith = "Increment:";
                Instance.ChangeElement<FunctionElement>(team.GetGroupName(), startsWith, (el) => el.Title = $"{startsWith} {GetIncrement(team.TeamName)}");

                team.Metadata._settingsList.Types(x =>
                {
                    var _x = x as ToggleServerSetting<float>;
                    Instance.ChangeElement<FloatElement>(team.GetGroupName(), _x.DisplayName, (el) => el.Increment = GetIncrement(team.TeamName));
                }, typeof(ToggleServerSetting<float>));
            });

            if (team == Instance.Infected)
            {
                group.AddElement("Add Infected Children Team", Instance.Config.AddInfectedChildrenTeam.Value, (val) =>
                {
                    Instance.Config.AddInfectedChildrenTeam.Value = val;
                    RefreshSettingsPage();
                });
            }

            return group;
        }

        private static float GetIncrement(string teamName)
        {
            if (IncrementTeams.TryGetValue(teamName, out int index))
                return IncrementValues[index];

            IncrementTeams[teamName] = 0;
            return IncrementValues[0];
        }

        private static void CreateStatElement(this GroupElementData group, InfectionTeam team, ToggleServerSetting<float> stat)
        {
            group.AddElement($"Override {stat.DisplayName}", stat.ClientEnabled, (val) => { stat.ClientEnabled = val; FormatApplyName(team); RefreshSettingsPage(); });
            if (stat.ClientEnabled)
                group.AddElement(stat.DisplayName, stat.ClientValue, (val) => { stat.ClientValue = val; FormatApplyName(team); }, increment: GetIncrement(team.TeamName));
        }

        private static void CreateStatElement(this GroupElementData group, InfectionTeam team, ServerSetting<bool> stat)
        {
            group.AddElement(stat.DisplayName, stat.ClientValue, (val) => { stat.ClientValue = val; FormatApplyName(team); });
        }

        internal static string FormatApplyName(InfectionTeam team, bool apply = true)
        {
            const string name = "Apply new settings";

            string _name;

            if (!Instance.IsStarted)
            {
                _name = $"<color=#000000>{name} (Gamemode not started)</color>";
            }
            else if (team.Metadata.IsApplied)
            {
                _name = $"<color=#00FF00>{name} (Applied)</color>";
            }
            else if (team.Metadata.HasNoServerSettings())
            {
                // For some fucking reason this happens, so I am making this to be able to tell when the code stops working, again.
                _name = $"<color=#898989>{name} (No Server Settings, fucked up code)</color>";
            }
            else
            {
                _name = $"<color=#FF0000>{name} (Not Applied)</color>";
            }

            if (apply)
                Instance.ChangeElement<FunctionElement>(team.GetGroupName(), name, (el) => el.Title = _name, false);

            return _name;
        }

        private static void ApplyMetadata(InfectionTeam team)
        {
            if (Instance.IsStarted)
            {
                if (!team.Metadata.IsApplied)
                {
                    team.Metadata.ApplyConfig();
                    EventManager.TryInvokeEvent(EventType.RefreshStats, team.TeamName);
                }

                FormatApplyName(team);
            }
        }

        private static bool HasNoServerSettings(this TeamMetadata metadata)
        {
            return !metadata._settingsList.Any(setting => setting is IServerSetting);
        }

        private static string GetGroupName(this InfectionTeam team)
            => string.Format(TeamConfigName, team.DisplayName);

        // For some reason, visual studio deems the suppression unnecessary, but if I remove it, it gives me a fucking warning, very logical.
        // Copilot stop trying to suggest me how to write commands. please.
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable S3011 // Make sure that this accessibility bypass is safe here

        internal static void RefreshSettingsPage()
        {
            if (MenuGamemode.SelectedGamemode != Instance)
                return;

            List<string> openGroups = [];
            MenuGamemode.SettingsPageElement.Elements.ForEach(x =>
            {
                if (x is DropdownElement group && group.Expanded)
                    openGroups.Add(group.Title);
            });

            typeof(MenuGamemode)
                .GetMethod("OverrideSettingsPage",
                            bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, [Instance]);

            MenuGamemode.SettingsPageElement.Elements.ForEach(x =>
            {
                if (x is DropdownElement group && !group.Expanded && openGroups.Contains(group.Title))
                    group.Expand();
            });
        }

#pragma warning restore S3011 // Make sure that this accessibility bypass is safe here
#pragma warning restore IDE0079 // Remove unnecessary suppression
    }
}