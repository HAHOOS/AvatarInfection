using System;
using System.Collections.Generic;

using AvatarInfection.Helper;
using AvatarInfection.Settings;

using BoneLib;

using LabFusion.Menu.Data;
using LabFusion.SDK.Gamemodes;

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
            var group = new GroupElementData()
            {
                Title = Infection.Instance.Title,
            };

            if (group == null)
            {
                FusionModule.Logger.Error("Group is null");
                return null;
            }

            GroupElementData avatarGroup = group.AddGroup("Avatar");

            avatarGroup.AddElement("Selected Avatar: N/A", () =>
            {
                if (Infection.Instance.IsStarted)
                    return;

                var rigManager = Player.RigManager;
                if (rigManager?.AvatarCrate?.Barcode != null)
                {
                    var avatar = rigManager.AvatarCrate.Barcode.ID;

                    if (string.IsNullOrWhiteSpace(avatar))
                        return;
                    Infection.Instance.Config.SelectedAvatar.ClientValue = avatar;

                    string title = !string.IsNullOrWhiteSpace(rigManager.AvatarCrate?.Scannable?.Title) ? rigManager.AvatarCrate.Scannable.Title : "N/A";

                    Infection.Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                        avatarGroup.Title,
                        "Selected Avatar:",
                        (element) => element.Title = $"Selected Avatar: {title}",
                    true);
                }
            });

            avatarGroup.AddElement("Select Mode", Infection.Instance.Config.SelectMode.Value, (val) => Infection.Instance.Config.SelectMode.Value = (AvatarSelectMode)val);

            group.AddElement(CreateElementsForTeam(Infection.Instance.Infected));

            group.AddElement(CreateElementsForTeam(Infection.Instance.InfectedChildren));

            group.AddElement(CreateElementsForTeam(Infection.Instance.Survivors));
            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", Infection.Instance.Config.InfectedCount.Value, (val) => Infection.Instance.Config.InfectedCount.Value = val, min: 1, max: 5);

            generalGroup.AddElement("Time Limit", Infection.Instance.Config.TimeLimit.Value, (val) =>
            {
                Infection.Instance.Config.TimeLimit.Value = val;
                if (Infection.Instance.IsStarted)
                    Infection.Instance.EndUnix.SetValue(DateTimeOffset.FromUnixTimeMilliseconds((long)Infection.Instance.StartUnix.GetValue()).AddMinutes(val).ToUnixTimeMilliseconds());
            }, min: 1);

            generalGroup.AddElement("Disable Spawn Gun", Infection.Instance.DisableSpawnGun, (val) => Infection.Instance.Config.DisableSpawnGun.ClientValue = val);

            generalGroup.AddElement("Disable Dev Tools", Infection.Instance.DisableDevTools, (val) => Infection.Instance.Config.DisableDevTools.ClientValue = val);

            generalGroup.AddElement("Allow Keep Inventory", Infection.Instance.Config.AllowKeepInventory.ClientValue, (val) => Infection.Instance.Config.AllowKeepInventory.ClientValue = val);

            generalGroup.AddElement("No Time Limit", Infection.Instance.Config.NoTimeLimit.Value, (val) =>
            {
                Infection.Instance.Config.NoTimeLimit.Value = val;
                if (Infection.Instance.IsStarted)
                    Infection.Instance.EndUnix.SetValue(-1);
            });

            generalGroup.AddElement("Use Deathmatch Spawns", Infection.Instance.Config.UseDeathmatchSpawns.ClientValue, (val) => Infection.Instance.Config.UseDeathmatchSpawns.ClientValue = val);

            generalGroup.AddElement("Teleport To Host On Start", Infection.Instance.Config.TeleportOnStart.Value, (val) => Infection.Instance.Config.TeleportOnStart.Value = val);

            generalGroup.AddElement("Teleport To Host On End", Infection.Instance.Config.TeleportOnEnd.ClientValue, (val) => Infection.Instance.Config.TeleportOnEnd.ClientValue = val);

            generalGroup.AddElement("Countdown Length", Infection.Instance.Config.CountdownLength.ClientValue, (val) => Infection.Instance.Config.CountdownLength.ClientValue = val, 5, 0, 3600);
            generalGroup.AddElement("Show Countdown to All Players", Infection.Instance.Config.ShowCountdownToAll.ClientValue, (val) => Infection.Instance.Config.ShowCountdownToAll.ClientValue = val);

            generalGroup.AddElement("Infect Type", Infection.Instance.Config.InfectType.Value, (val) => Infection.Instance.Config.InfectType.Value = (InfectType)val);

            generalGroup.AddElement("Suicide Infects", Infection.Instance.Config.SuicideInfects.Value, (val) => Infection.Instance.Config.SuicideInfects.Value = val);
            generalGroup.AddElement("Hold Time (Touch Infect Type)", Infection.Instance.Config.HoldTime.Value, (val) => Infection.Instance.Config.HoldTime.Value = val, max: 60);
            generalGroup.AddElement("Save Settings", Infection.Instance.Config.Save);
            generalGroup.AddElement("Load Settings", () =>
            {
                Infection.Instance.Config.Load();
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
                typeof(LabFusion.Menu.Gamemodes.MenuGamemode)
                    .GetMethod("OverrideSettingsPage",
                                bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, [(Gamemode)Infection.Instance]);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            });

            return group;
        }

        internal static GroupElementData CreateElementsForTeam(InfectionTeam team)
        {
            var group = new GroupElementData()
            {
                Title = $"{team.Team.DisplayName} Stats"
            };

            void applyButtonUpdate()
            {
                const string name = "Apply new settings";
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    string.Format(TeamConfigName, Instance.Infected.Team.DisplayName),
                    "Apply new settings", (el) => el.Title = team.Metadata.IsApplied ? $"<color=#FF0000>{name}</color>" : name, true);
            }

            group.AddElement("Apply new settings (use when the gamemode is already started)", () =>
            {
                if (Instance.IsStarted)
                {
                    var _metadata = team.Metadata;
                    if (team.Team == Instance.InfectedChildren.Team && Instance.Config.SyncWithInfected.Value)
                        _metadata = Instance.Infected.Metadata;

                    if (_metadata.IsApplied)
                        return;
                    _metadata.ApplyConfig();
                    EventManager.TryInvokeEvent(EventType.RefreshStats, team);
                }
            });
            group.AddElement("Mortality", team.Metadata.Mortality.ClientValue, (val) => { team.Metadata.Mortality.ClientValue = val; applyButtonUpdate(); });
            group.AddElement("Can Use Guns", team.Metadata.CanUseGuns.ClientValue, (val) => { team.Metadata.CanUseGuns.ClientValue = val; applyButtonUpdate(); });
            group.AddElement("Override Vitality", team.Metadata.Vitality.ClientEnabled, (val) => { team.Metadata.Vitality.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Vitality", team.Metadata.Vitality.ClientValue, (val) => { team.Metadata.Vitality.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Speed", team.Metadata.Speed.ClientEnabled, (val) => { team.Metadata.Speed.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Speed", team.Metadata.Speed.ClientValue, (val) => { team.Metadata.Speed.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Agility", team.Metadata.Agility.ClientEnabled, (val) => { team.Metadata.Agility.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Agility", team.Metadata.Agility.ClientValue, (val) => { team.Metadata.Agility.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Strength Upper", team.Metadata.StrengthUpper.ClientEnabled, (val) => { team.Metadata.StrengthUpper.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Strength Upper", team.Metadata.StrengthUpper.ClientValue, (val) => { team.Metadata.StrengthUpper.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement($"Increment: {Increment}", () =>
            {
                var group = string.Format(TeamConfigName, team.Metadata.Team.DisplayName);

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
            });

            if (team.Team == Instance.InfectedChildren.Team)
                group.AddElement("Sync with Infected", Instance.Config.SyncWithInfected.Value, (val) => Instance.Config.SyncWithInfected.Value = val);

            return group;
        }
    }
}