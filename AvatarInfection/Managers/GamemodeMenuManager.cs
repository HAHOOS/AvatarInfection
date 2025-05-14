using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Helper;

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

            avatarGroup.AddElement("Select Mode", Infection.Instance.Config.SelectMode, Instance, (val) => Infection.Instance.Config.SelectMode = (AvatarSelectMode)val);

            group.AddElement(CreateElementsForTeam(Infection.TeamEnum.Infected));

            group.AddElement(CreateElementsForTeam(Infection.TeamEnum.InfectedChildren));

            group.AddElement(CreateElementsForTeam(Infection.TeamEnum.Survivors));
            GroupElementData generalGroup = group.AddGroup("General");

            generalGroup.AddElement("Infected Start Number", Infection.Instance.Config.InfectedCount, (val) => Infection.Instance.Config.InfectedCount = val, min: 1, max: 5);

            generalGroup.AddElement("Time Limit", Infection.Instance.Config.TimeLimit, (val) =>
            {
                Infection.Instance.Config.TimeLimit = val;
                if (Infection.Instance.IsStarted)
                    Infection.Instance.EndUnix.SetValue(DateTimeOffset.FromUnixTimeMilliseconds((long)Infection.Instance.StartUnix.GetValue()).AddMinutes(val).ToUnixTimeMilliseconds());
            }, min: 1);

            generalGroup.AddElement("Disable Spawn Gun", Infection.Instance.DisableSpawnGun, (val) => Infection.Instance.Config.DisableSpawnGun.ClientValue = val);

            generalGroup.AddElement("Disable Dev Tools", Infection.Instance.DisableDevTools, (val) => Infection.Instance.Config.DisableDevTools.ClientValue = val);

            generalGroup.AddElement("Allow Keep Inventory", Infection.Instance.Config.AllowKeepInventory.ClientValue, (val) => Infection.Instance.Config.AllowKeepInventory.ClientValue = val);

            generalGroup.AddElement("No Time Limit", Infection.Instance.Config.NoTimeLimit, (val) =>
            {
                Infection.Instance.Config.NoTimeLimit = val;
                if (Infection.Instance.IsStarted)
                    Infection.Instance.EndUnix.SetValue(-1);
            });

            generalGroup.AddElement("Use Deathmatch Spawns", Infection.Instance.Config.UseDeathmatchSpawns.ClientValue, (val) => Infection.Instance.Config.UseDeathmatchSpawns.ClientValue = val);

            generalGroup.AddElement("Teleport To Host On Start", Infection.Instance.Config.TeleportOnStart, (val) => Infection.Instance.Config.TeleportOnStart = val);

            generalGroup.AddElement("Teleport To Host On End", Infection.Instance.Config.TeleportOnEnd.ClientValue, (val) => Infection.Instance.Config.TeleportOnEnd.ClientValue = val);

            generalGroup.AddElement("Countdown Length", Infection.Instance.Config.CountdownLength.ClientValue, (val) => Infection.Instance.Config.CountdownLength.ClientValue = val, 5, 0, 3600);
            generalGroup.AddElement("Show Countdown to All Players", Infection.Instance.Config.ShowCountdownToAll.ClientValue, (val) => Infection.Instance.Config.ShowCountdownToAll.ClientValue = val);

            generalGroup.AddElement("Infect Type", Infection.Instance.Config.InfectType, Instance, (val) => Infection.Instance.Config.InfectType = (InfectTypeEnum)val);

            generalGroup.AddElement("Suicide Infects", Infection.Instance.Config.SuicideInfects, (val) => Infection.Instance.Config.SuicideInfects = val);
            generalGroup.AddElement("Hold Time (Touch Infect Type)", Infection.Instance.Config.HoldTime, (val) => Infection.Instance.Config.HoldTime = val, max: 60);

            return group;
        }

        internal static GroupElementData CreateElementsForTeam(TeamEnum team)
        {
            var metadata = team == TeamEnum.Infected ? Infection.Instance.InfectedMetadata :
                team == TeamEnum.Survivors ? Infection.Instance.SurvivorsMetadata : Infection.Instance.InfectedChildrenMetadata;
            var group = new GroupElementData()
            {
                Title = $"{(team == TeamEnum.Survivors ? "Survivors Stats"
                   : team == TeamEnum.InfectedChildren ? "Infected Children Stats" : "Infected Stats")}"
            };

            void applyButtonUpdate()
            {
                const string name = "Apply new settings";
                Instance.ChangeElement<LabFusion.Marrow.Proxies.FunctionElement>(
                    string.Format(TeamConfigName, Infection.Instance.Infected.DisplayName),
                    "Apply new settings", (el) => el.Title = metadata.IsApplied ? $"<color=#FF0000>{name}</color>" : name, true);
            }

            group.AddElement("Apply new settings (use when the gamemode is already started)", () =>
            {
                if (Infection.Instance.IsStarted)
                {
                    var _metadata = metadata;
                    if (team == TeamEnum.InfectedChildren && Infection.Instance.Config.SyncWithInfected)
                        _metadata = Infection.Instance.InfectedMetadata;

                    if (_metadata.IsApplied)
                        return;
                    _metadata.ApplyConfig();
                    EventManager.TryInvokeEvent(EventType.RefreshStats, team);
                }
            });
            group.AddElement("Mortality", metadata.Mortality.ClientValue, (val) => { metadata.Mortality.ClientValue = val; applyButtonUpdate(); });
            group.AddElement("Can Use Guns", metadata.CanUseGuns.ClientValue, (val) => { metadata.CanUseGuns.ClientValue = val; applyButtonUpdate(); });
            group.AddElement("Override Vitality", metadata.Vitality.ClientEnabled, (val) => { metadata.Vitality.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Vitality", metadata.Vitality.ClientValue, (val) => { metadata.Vitality.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Speed", metadata.Speed.ClientEnabled, (val) => { metadata.Speed.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Speed", metadata.Speed.ClientValue, (val) => { metadata.Speed.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Agility", metadata.Agility.ClientEnabled, (val) => { metadata.Agility.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Agility", metadata.Agility.ClientValue, (val) => { metadata.Agility.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement("Override Strength Upper", metadata.StrengthUpper.ClientEnabled, (val) => { metadata.StrengthUpper.ClientEnabled = val; applyButtonUpdate(); });
            group.AddElement("Strength Upper", metadata.StrengthUpper.ClientValue, (val) => { metadata.StrengthUpper.ClientValue = val; applyButtonUpdate(); }, increment: Increment);
            group.AddElement($"Increment: {Increment}", () =>
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
            });

            if (team == TeamEnum.InfectedChildren)
                group.AddElement("Sync with Infected", Infection.Instance.Config.SyncWithInfected, (val) => Infection.Instance.Config.SyncWithInfected = val);

            return group;
        }
    }
}