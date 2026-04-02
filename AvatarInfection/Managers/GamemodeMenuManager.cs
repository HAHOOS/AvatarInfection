using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using LabFusion.Marrow.Proxies;
using LabFusion.Menu.Data;
using LabFusion.Menu.Gamemodes;
using LabFusion.Network;
using LabFusion.SDK.Gamemodes;

using static AvatarInfection.Infection;

namespace AvatarInfection.Managers
{
    internal static class GamemodeMenuManager
    {
        private const string TeamConfigName = "{0} Stats";

        private static readonly Dictionary<string, int> IncrementTeams = [];

        private static readonly IReadOnlyList<float> IncrementValues = [0.2f, 0.5f, 1f, 5f];

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

            Instance.Config._settingsList.ForEach(x =>
            {
                if (x is AvatarSetting avSetting)
                    group.AddElement(avSetting.CreateGroup());
            });

            group.AddElement(CreateElementsForTeam(Instance.Infected));
            group.AddElement(CreateElementsForTeam(Instance.InfectedChildren));
            group.AddElement(CreateElementsForTeam(Instance.Survivors));

            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", Instance.Config.InfectedCount.Value, (val) => Instance.Config.InfectedCount.Value = val, min: 1, max: 5);

            generalGroup.AddElement("No Time Limit", Instance.Config.NoTimeLimit.Value, (val) =>
            {
                Instance.Config.NoTimeLimit.Value = val;
                if (Instance.IsStarted)
                    Instance.Config.EndUnix.Value = -1;
                RefreshSettingsPage();
            });

            if (!Instance.Config.NoTimeLimit.Value)
                generalGroup.AddElement("Time Limit", Instance.Config.TimeLimit.Value, SetTimeLimit, min: 1);

            generalGroup.AddElement("Friendly Fire", Instance.Config.FriendlyFire.Value, (val) => Instance.Config.FriendlyFire.Value = val);

            generalGroup.AddElement("Dont Repeat Infected", Instance.Config.DontRepeatInfected.Value, (val) => Instance.Config.DontRepeatInfected.Value = val);

            generalGroup.AddElement("Disable Spawn Gun", Instance.DisableSpawnGun, (val) => Instance.Config.DisableSpawnGun.Value = val);

            generalGroup.AddElement("Disable Developer Tools", Instance.DisableDevTools, (val) => Instance.Config.DisableDevTools.Value = val);

            generalGroup.AddElement("Allow Keep Inventory", Instance.Config.AllowKeepInventory.Value, (val) => Instance.Config.AllowKeepInventory.Value = val);

            generalGroup.AddElement("Use DeathMatch Spawns", Instance.Config.UseDeathmatchSpawns.Value, (val) => Instance.Config.UseDeathmatchSpawns.Value = val);

            generalGroup.AddElement("Teleport To Host On Start", Instance.Config.TeleportOnStart.Value, (val) => Instance.Config.TeleportOnStart.Value = val);

            generalGroup.AddElement("Teleport To Host On End", Instance.Config.TeleportOnEnd.Value, (val) => Instance.Config.TeleportOnEnd.Value = val);

            generalGroup.AddElement("Countdown Length", Instance.Config.CountdownLength.Value, SetCountdownLength, 5, 0, 3600);
            if (Instance.Config.CountdownLength.Value > 0)
                generalGroup.AddElement("Show Countdown to All Players", Instance.Config.ShowCountdownToAll.Value, (val) => Instance.Config.ShowCountdownToAll.Value = val);

            generalGroup.AddElement("Infect Type", Instance.Config.InfectType.Value, (val) => { Instance.Config.InfectType.Value = (InfectType)val; RefreshSettingsPage(); });

            if (Instance.Config.InfectType.Value == InfectType.TOUCH)
                generalGroup.AddElement("Hold Time", Instance.Config.HoldTime.Value, (val) => Instance.Config.HoldTime.Value = val, max: 60);

            generalGroup.AddElement("Suicide Infects", Instance.Config.SuicideInfects.Value, (val) => Instance.Config.SuicideInfects.Value = val);
            generalGroup.AddElement("Save Settings", SaveSettings);
            generalGroup.AddElement("Load Settings", LoadSettings);

            return group;
        }

        private static void SaveSettings()
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
        }

        private static void LoadSettings()
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
        }

        private static void SetTimeLimit(int val)
        {
            Instance.Config.TimeLimit.Value = val;
            if (Instance.IsStarted)
                Instance.Config.EndUnix.Value = DateTimeOffset.FromUnixTimeMilliseconds(Instance.Config.StartUnix.Value).AddMinutes(val).ToUnixTimeMilliseconds();
        }

        private static void SetCountdownLength(int val)
        {
            var old = Instance.Config.CountdownLength.Value;
            Instance.Config.CountdownLength.Value = val;
            if ((old == 0 && val > 0) || (old > 0 && val == 0))
                RefreshSettingsPage();
        }

        internal static GroupElementData CreateElementsForTeam(InfectionTeam team)
        {
            var group = new GroupElementData()
            {
                Title = team.GetGroupName(),
            };

            bool isChildren = team == Instance.InfectedChildren;
            if (isChildren)
            {
                group.AddElement("Sync With Infected", Instance.Config.SyncWithInfected.Value, (val) =>
                {
                    Instance.Config.SyncWithInfected.Value = val;
                    RefreshSettingsPage();
                });
            }

            if (!isChildren || (isChildren && !Instance.Config.SyncWithInfected.Value))
            {
                if (Instance.IsStarted)
                    group.AddElement(FormatApplyName(team, apply: false), () => ApplyMetadata(team));

                var incrementElem = IncrementElem(team.TeamName);
                group.AddElement(incrementElem);

                team.StaticMetadata._settingsList.Types(x =>
                {
                    if (x is ToggleServerSetting<float> toggleStat)
                        group.CreateStatElement(team, toggleStat);
                    else if (x is ServerSetting<bool> boolStat)
                        group.CreateStatElement(boolStat);
                }, typeof(ToggleServerSetting<float>), typeof(ServerSetting<bool>));

                group.AddElement(incrementElem);
            }

            return group;
        }

        private static FunctionElementData IncrementElem(string teamName)
        {
            return new FunctionElementData()
            {
                Title = $"Increment: {GetIncrement(teamName)}",
                OnPressed = () =>
            {
                if (!IncrementTeams.TryGetValue(teamName, out int index))
                    index = 0;

                index++;
                index %= IncrementValues.Count;
                IncrementTeams[teamName] = index;

                RefreshSettingsPage();
            }
            };
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
            group.AddElement($"Override {stat.DisplayName}", stat.Enabled, (val) => { stat.Enabled = val; RefreshSettingsPage(); });
            if (stat.Enabled)
                group.AddElement(stat.DisplayName, stat.Value, (val) => stat.Value = val, increment: GetIncrement(team.TeamName));
        }

        private static void CreateStatElement(this GroupElementData group, ServerSetting<bool> stat)
            => group.AddElement(stat.DisplayName, stat.Value, (val) => stat.Value = val);

        internal static string FormatApplyName(InfectionTeam team, bool apply = true)
        {
            const string name = "Apply new settings";

            string _name;

            if (team.Metadata.IsApplied)
                _name = $"<color=#00FF00>{name} (Applied)</color>"; // green color
            else
                _name = $"<color=#FF0000>{name} (Not Applied)</color>"; // red color

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

            Internal_Refresh();

            MenuGamemode.SettingsPageElement.Elements.ForEach(x =>
            {
                if (x is DropdownElement group && !group.Expanded && openGroups.Contains(group.Title))
                    group.Expand();
            });
        }

        internal static void Internal_Refresh()
        {
            MenuGamemode.SettingsPageElement.Clear();

            MenuGamemode.SettingsGrid.SetActive(true);

            if (NetworkInfo.IsHost)
            {
                typeof(MenuGamemode)
                .GetMethod("ApplySettingsData",
                            bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, [Instance]);
            }
        }

#pragma warning restore S3011 // Make sure that this accessibility bypass is safe here
#pragma warning restore IDE0079 // Remove unnecessary suppression
    }
}