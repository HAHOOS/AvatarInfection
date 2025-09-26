using System.Collections.Generic;
using System.Linq;

using BoneLib.BoneMenu;

using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
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

            Dictionary<PlayerID, Team> Teams = [];

            foreach (var player in PlayerIDManager.PlayerIDs)
            {
                var team = Infection.Instance.TeamManager.GetPlayerTeam(player);
                Teams.Add(player, team);
            }

            var infected = Teams.Any(x => x.Value == Infection.Instance.Infected) ?
                ModPage.CreatePage($"Infected ({Teams.Count(x => x.Value == Infection.Instance.Infected)})", Color.green) : null;
            var children = Teams.Any(x => x.Value == Infection.Instance.InfectedChildren) ?
                ModPage.CreatePage($"Infected Children ({Teams.Count(x => x.Value == Infection.Instance.InfectedChildren)})", new Color(0, 1, 0)) : null;
            var survivors = Teams.Any(x => x.Value == Infection.Instance.Survivors) ?
                ModPage.CreatePage($"Survivors ({Teams.Count(x => x.Value == Infection.Instance.Survivors)})", Color.cyan) : null;

            var unidentified = Teams.Any(x => x.Value == null) ?
                ModPage.CreatePage($"Unidentified ({Teams.Count(x => x.Value == null)})", Color.gray) : null;

            foreach (var team in Teams)
            {
                if (!team.Key.TryGetDisplayName(out var displayName))
                    continue;

                BoneLib.BoneMenu.Page page;

                if (team.Value == Infection.Instance.Infected)
                    page = infected;
                else if (team.Value == Infection.Instance.InfectedChildren)
                    page = children;
                else if (team.Value == Infection.Instance.Survivors)
                    page = survivors;
                else
                    page = unidentified;

                if (page == null)
                    continue;

                page.CreateFunction(displayName, Color.white, null);
            }
        }
    }
}