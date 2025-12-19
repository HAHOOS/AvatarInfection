using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using BoneLib;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Marrow.Proxies;
using LabFusion.Menu.Data;

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

            GroupElementData avatarGroup = group.AddGroup("Avatar");

            avatarGroup.AddElement($"Selected Avatar: {GetBarcodeTitle(Instance.Config.SelectedAvatar.ClientValue)}", SelectNewAvatar);

            avatarGroup.AddElement("Select Mode", Instance.Config.SelectMode.Value, (val) => Instance.Config.SelectMode.Value = (AvatarSelectMode)val);

            group.AddElement(CreateElementsForTeam(Instance.Infected));

            if (Instance.Config.AddInfectedChildrenTeam.Value)
                group.AddElement(CreateElementsForTeam(Instance.InfectedChildren));

            group.AddElement(CreateElementsForTeam(Instance.Survivors));
            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", Instance.Config.InfectedCount.Value, (val) => Instance.Config.InfectedCount.Value = val, min: 1, max: 5);

            generalGroup.AddElement("Time Limit", Instance.Config.TimeLimit.Value, (val) =>
            {
                Instance.Config.TimeLimit.Value = val;
                if (Instance.IsStarted)
                    Instance.EndUnix.SetValue(DateTimeOffset.FromUnixTimeMilliseconds((long)Instance.StartUnix.GetValue()).AddMinutes(val).ToUnixTimeMilliseconds());
            }, min: 1);

            generalGroup.AddElement("Disable Spawn Gun", Instance.DisableSpawnGun, (val) => Instance.Config.DisableSpawnGun.ClientValue = val);

            generalGroup.AddElement("Disable Developer Tools", Instance.DisableDevTools, (val) => Instance.Config.DisableDevTools.ClientValue = val);

            generalGroup.AddElement("Allow Keep Inventory", Instance.Config.AllowKeepInventory.ClientValue, (val) => Instance.Config.AllowKeepInventory.ClientValue = val);

            generalGroup.AddElement("No Time Limit", Instance.Config.NoTimeLimit.Value, (val) =>
            {
                Instance.Config.NoTimeLimit.Value = val;
                if (Instance.IsStarted)
                    Instance.EndUnix.SetValue(-1);
            });

            generalGroup.AddElement("Use DeathMatch Spawns", Instance.Config.UseDeathmatchSpawns.ClientValue, (val) => Instance.Config.UseDeathmatchSpawns.ClientValue = val);

            generalGroup.AddElement("Teleport To Host On Start", Instance.Config.TeleportOnStart.Value, (val) => Instance.Config.TeleportOnStart.Value = val);

            generalGroup.AddElement("Teleport To Host On End", Instance.Config.TeleportOnEnd.ClientValue, (val) => Instance.Config.TeleportOnEnd.ClientValue = val);

            generalGroup.AddElement("Countdown Length", Instance.Config.CountdownLength.ClientValue, (val) => Instance.Config.CountdownLength.ClientValue = val, 5, 0, 3600);
            generalGroup.AddElement("Show Countdown to All Players", Instance.Config.ShowCountdownToAll.ClientValue, (val) => Instance.Config.ShowCountdownToAll.ClientValue = val);

            generalGroup.AddElement("Infect Type", Instance.Config.InfectType.Value, (val) => Instance.Config.InfectType.Value = (InfectType)val);

            generalGroup.AddElement("Suicide Infects", Instance.Config.SuicideInfects.Value, (val) => Instance.Config.SuicideInfects.Value = val);
            generalGroup.AddElement("Hold Time (Touch Infect Type)", Instance.Config.HoldTime.Value, (val) => Instance.Config.HoldTime.Value = val, max: 60);
            generalGroup.AddElement("Save Settings", () =>
            {
                try
                {
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

                const string startsWith = "Selected Avatar:";
                Instance.ChangeElement<FunctionElement>("Avatar", startsWith, (element) => element.Title = $"{startsWith} {title}");
            }
        }

        private static string GetBarcodeTitle(string barcode)
            => !string.IsNullOrWhiteSpace(barcode) ? (new AvatarCrateReference(barcode).Crate.Title ?? "N/A") : "N/A";

        internal static GroupElementData CreateElementsForTeam(InfectionTeam team)
        {
            var group = new GroupElementData()
            {
                Title = string.Format(TeamConfigName, team.Team.DisplayName),
            };

            group.AddElement(FormatApplyName(team, apply: false), () => ApplyMetadata(team));

            team.Metadata._settingsList.Types(x =>
            {
                if (x is ToggleServerSetting<float> toggleStat)
                    group.CreateStatElement(team, toggleStat);
                else if (x is ServerSetting<bool> boolStat)
                    group.CreateStatElement(team, boolStat);
            }, typeof(ToggleServerSetting<float>), typeof(ServerSetting<bool>));

            group.AddElement($"Increment: {GetIncrement(team.Team.TeamName)}", () =>
            {
                var group = string.Format(TeamConfigName, team.Team.DisplayName);

                if (!IncrementTeams.TryGetValue(team.Team.TeamName, out int index))
                    index = 0;

                index++;
                index %= IncrementValues.Count;
                IncrementTeams[team.Team.TeamName] = index;

                Instance.ChangeElement<FunctionElement>(group, "Increment:", (el) => el.Title = $"Increment: {GetIncrement(team.Team.TeamName)}");

                team.Metadata._settingsList.Types(x =>
                {
                    var _x = x as ToggleServerSetting<float>;
                    Instance.ChangeElement<FloatElement>(group, _x.DisplayName, (el) => el.Increment = GetIncrement(team.Team.TeamName));
                }, typeof(ToggleServerSetting<float>));
            });

            if (team.Team == Instance.Infected.Team)
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
            group.AddElement($"Override {stat.DisplayName}", stat.ClientEnabled, (val) => { stat.ClientEnabled = val; FormatApplyName(team); });
            group.AddElement(stat.DisplayName, stat.ClientValue, (val) => { stat.ClientValue = val; FormatApplyName(team); }, increment: GetIncrement(team.Team.TeamName));
        }

        private static void CreateStatElement(this GroupElementData group, InfectionTeam team, ServerSetting<bool> stat)
        {
            group.AddElement(stat.DisplayName, stat.ClientValue, (val) => { stat.ClientValue = val; FormatApplyName(team); });
        }

        private static string FormatApplyName(InfectionTeam team, bool apply = true)
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
            {
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    string.Format(TeamConfigName, team.Team.DisplayName),
                    name, (el) => el.Title = _name, true);
            }

            return _name;
        }

        private static void ApplyMetadata(InfectionTeam team)
        {
            if (Instance.IsStarted)
            {
                var _metadata = team.Metadata;
                if (team.Team == Instance.InfectedChildren.Team && !Instance.Config.AddInfectedChildrenTeam.Value)
                    _metadata = Instance.Infected.Metadata;

                if (_metadata.IsApplied)
                    return;

                _metadata.ApplyConfig();
                EventManager.TryInvokeEvent(EventType.RefreshStats, team.Team.TeamName);
            }
        }

        private static bool HasNoServerSettings(this TeamMetadata metadata)
        {
            return !metadata._settingsList.Any(setting => setting is IServerSetting);
        }

        // For some reason, visual studio deems the suppression unnecessary, but if I remove it, it gives me a fucking warning, very logical.
        // Copilot stop trying to suggest me how to write commands. pretty please.
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable S3011 // Make sure that this accessibility bypass is safe here
        private static void RefreshSettingsPage() => typeof(LabFusion.Menu.Gamemodes.MenuGamemode)
                .GetMethod("OverrideSettingsPage",
                            bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, [Instance]);

#pragma warning restore S3011 // Make sure that this accessibility bypass is safe here
#pragma warning restore IDE0079 // Remove unnecessary suppression
    }
}