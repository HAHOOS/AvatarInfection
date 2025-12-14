using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using BoneLib;

using LabFusion.Menu.Data;

using static AvatarInfection.Infection;

namespace AvatarInfection.Managers
{
    internal static class GamemodeMenuManager
    {
        private const string TeamConfigName = "{0} Config";

        internal static float Increment
        {
            get
            {
                return IncrementValues[IncrementIndex];
            }
        }

        private static readonly IReadOnlyList<float> IncrementValues = [0.2f, 0.5f, 1f, 5f];
        private static int IncrementIndex = 0;

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

            avatarGroup.AddElement("Selected Avatar: N/A", SelectNewAvatar);

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
            generalGroup.AddElement("Save Settings", Instance.Config.Save);
            generalGroup.AddElement("Load Settings", () =>
            {
                Instance.Config.Load();
                RefreshSettingsPage();
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

                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    "Avatar",
                    "Selected Avatar:",
                    (element) => element.Title = $"Selected Avatar: {title}",
                true);
            }
        }

        internal static GroupElementData CreateElementsForTeam(InfectionTeam team)
        {
            var group = new GroupElementData()
            {
                Title = $"{team.Team.DisplayName} Stats"
            };

            group.AddElement("Apply new settings (use when the gamemode is already started)", () =>
            {
                if (Instance.IsStarted)
                {
                    var _metadata = team.Metadata;
                    if (team.Team == Instance.InfectedChildren.Team && !Instance.Config.AddInfectedChildrenTeam.Value)
                        _metadata = Instance.Infected.Metadata;

                    if (_metadata.IsApplied)
                        return;
                    _metadata.ApplyConfig();
                    EventManager.TryInvokeEvent(EventType.RefreshStats, team);
                }
            });

            Console.WriteLine($"Creating settings for team: {team.Team.DisplayName}");
            Console.WriteLine($" -> Mortality: {team.Metadata.Mortality.ClientValue}");
            Console.WriteLine($" -> CanUseGuns: {team.Metadata.CanUseGuns.ClientValue}");

            Console.WriteLine($" -> Vitality: {team.Metadata.Vitality.ClientValue}");
            Console.WriteLine($" -> Speed: {team.Metadata.Speed.ClientValue}");
            Console.WriteLine($" -> Agility: {team.Metadata.Agility.ClientValue}");
            Console.WriteLine($" -> StrengthUpper: {team.Metadata.StrengthUpper.ClientValue}");

            group.CreateStatElement(team, team.Metadata.Mortality);
            group.CreateStatElement(team, team.Metadata.CanUseGuns);
            group.CreateStatElement(team, team.Metadata.Vitality);
            group.CreateStatElement(team, team.Metadata.Speed);
            group.CreateStatElement(team, team.Metadata.Agility);
            group.CreateStatElement(team, team.Metadata.StrengthUpper);
            group.AddElement($"Increment: {Increment}", () =>
            {
                var group = string.Format(TeamConfigName, team.Team.DisplayName);

                IncrementIndex++;
                IncrementIndex %= IncrementValues.Count;
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    group,
                    "Increment:",
                    (el) => el.Title = $"Increment: {Increment}");

                Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, team.Metadata.StrengthUpper.GetTeamStatName(), (el) => el.Increment = Increment);
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, team.Metadata.Agility.GetTeamStatName(), (el) => el.Increment = Increment);
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, team.Metadata.Speed.GetTeamStatName(), (el) => el.Increment = Increment);
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FloatElement>(group, team.Metadata.Vitality.GetTeamStatName(), (el) => el.Increment = Increment);
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

        private static void CreateStatElement(this GroupElementData group, InfectionTeam team, ToggleServerSetting<float> stat)
        {
            group.AddElement($"Override {stat.GetTeamStatName()}", stat.ClientEnabled, (val) => { stat.ClientEnabled = val; ApplyButtonUpdate(team); });
            group.AddElement(stat.GetTeamStatName(), stat.ClientValue, (val) => { stat.ClientValue = val; ApplyButtonUpdate(team); }, increment: Increment);
        }

        private static void CreateStatElement(this GroupElementData group, InfectionTeam team, ServerSetting<bool> stat)
        {
            group.AddElement(stat.GetTeamStatName(), stat.ClientValue, (val) => { stat.ClientValue = val; ApplyButtonUpdate(team); });
        }

        public static string GetTeamStatName<T>(this ServerSetting<T> stat) where T : IEquatable<T>
            => stat.Name.Contains('_') ? stat.Name.Split('_').LastOrDefault() ?? "N/A" : stat.Name;

        public static string GetTeamStatName<T>(this ToggleServerSetting<T> stat) where T : IEquatable<T>
            => stat.Name.Contains('_') ? stat.Name.Split('_').LastOrDefault() ?? "N/A" : stat.Name;

        private static void ApplyButtonUpdate(InfectionTeam team)
        {
            const string name = "Apply new settings";
            Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                string.Format(TeamConfigName, team.Team.DisplayName),
                name, (el) => el.Title = team.Metadata.IsApplied ? $"<color=#FF0000>{name}</color>" : name, true);
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