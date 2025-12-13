using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Settings;

using BoneLib.BoneMenu;

using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Utilities;

using UnityEngine;

namespace AvatarInfection.Managers
{
    internal static class BoneMenuManager
    {
        public static Page AuthorPage { get; private set; }
        public static Page ModPage { get; private set; }

        public static void Setup()
        {
            AuthorPage = Page.Root.CreatePage("HAHOOS", Color.white);
            ModPage = AuthorPage.CreatePage("AvatarInfection", Color.magenta);
            PopulatePage();

            MultiplayerHooking.OnDisconnected += PopulatePage;
            MultiplayerHooking.OnJoinedServer += PopulatePage;
            MultiplayerHooking.OnStartedServer += PopulatePage;
            MultiplayerHooking.OnPlayerJoined += Hook;
            MultiplayerHooking.OnPlayerLeft += Hook;
        }

        public static void Destroy()
        {
            Menu.DestroyPage(ModPage);
            ModPage = null;

            MultiplayerHooking.OnDisconnected -= PopulatePage;
            MultiplayerHooking.OnJoinedServer -= PopulatePage;
            MultiplayerHooking.OnStartedServer -= PopulatePage;
            MultiplayerHooking.OnPlayerJoined -= Hook;
            MultiplayerHooking.OnPlayerLeft -= Hook;
        }

        private static void Hook(PlayerID _) => PopulatePage();

        public static void PopulatePage()
        {
            if (AuthorPage == null || ModPage == null)
                return;

            if (Infection.Instance == null)
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

            if (!Infection.Instance.IsStarted)
            {
                var label = ModPage.CreateFunction("Gamemode is not started :(", Color.white, null);
                label.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);
                return;
            }

            Dictionary<PlayerID, InfectionTeam> Teams = [];

            foreach (var player in PlayerIDManager.PlayerIDs)
            {
                var team = Infection.Instance.TeamManager.GetPlayerTeam(player);
                Teams.Add(player, team);
            }

            List<TeamPage> teamPages = [];

            teamPages.Add(Teams.Any(x => x.Value == Infection.Instance.Infected) ?
                new(ModPage.CreatePage($"Infected ({Teams.Count(x => x.Value == Infection.Instance.Infected)})", Color.green), Infection.Instance.Infected) : null);
            teamPages.Add(Teams.Any(x => x.Value == Infection.Instance.InfectedChildren) ?
                new(ModPage.CreatePage($"Infected Children ({Teams.Count(x => x.Value == Infection.Instance.InfectedChildren)})", new Color(0, 1, 0)), Infection.Instance.InfectedChildren) : null);

            teamPages.Add(Teams.Any(x => x.Value == Infection.Instance.Survivors) ?
                new(ModPage.CreatePage($"Survivors ({Teams.Count(x => x.Value == Infection.Instance.Survivors)})", Color.cyan), Infection.Instance.Survivors) : null);

            teamPages.Add(Teams.Any(x => x.Value == null) ?
                new(ModPage.CreatePage($"Unidentified ({Teams.Count(x => x.Value == null)})", Color.gray), null) : null);

            foreach (var team in Teams)
            {
                if (!team.Key.TryGetDisplayName(out var displayName))
                    continue;

                BoneLib.BoneMenu.Page page = teamPages.FirstOrDefault(x => (x.Team == null && team.Value == null) || (x.Team?.Team?.TeamName == team.Value?.Team?.TeamName))?.Page;

                if (page == null)
                    continue;

                page.CreateFunction(displayName, Color.white, null);
            }
        }
    }

    internal class TeamPage(Page page, InfectionTeam team)
    {
        public Page Page { get; private set; } = page;

        public InfectionTeam Team { get; private set; } = team;
    }
}