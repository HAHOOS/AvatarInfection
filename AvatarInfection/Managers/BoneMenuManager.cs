using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Settings;

using BoneLib.BoneMenu;

using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;

using UnityEngine;

using Page = BoneLib.BoneMenu.Page;

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

        private static readonly Dictionary<PlayerID, Team> Teams = [];

        public static void PopulatePage()
        {
            if (ModPage == null)
                return;

            if (Infection.Instance == null)
                return;

            ModPage.RemoveAll();
            ModPage.CreateFunction("Refresh", Color.cyan, PopulatePage);
            var seperator = ModPage.CreateFunction("[===============]", Color.magenta, null);
            seperator.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);

            if (!NetworkInfo.HasServer)
            {
                CreateErrorMessage("You aren't in any server :(");
                return;
            }

            if (!Infection.Instance.IsStarted)
            {
                CreateErrorMessage("Gamemode is not started :(");
                return;
            }

            Teams.Clear();

            foreach (var player in PlayerIDManager.PlayerIDs)
                Teams.Add(player, Infection.Instance?.TeamManager?.GetPlayerTeam(player)?.Team);


            List<TeamPage> teamPages = [];

            teamPages.Add(CreateTeamPage(Infection.Instance.Infected.Team));
            teamPages.Add(CreateTeamPage(Infection.Instance.InfectedChildren.Team));

            teamPages.Add(CreateTeamPage(Infection.Instance.Survivors.Team));

            teamPages.Add(CreateTeamPage(null));

            foreach (var player in Teams)
            {
                if (!player.Key.TryGetDisplayName(out var displayName))
                    continue;

                var team = teamPages?.FirstOrDefault(x => x?.Team == player.Value);

                if (team == null)
                    continue;

                team.Page.CreateFunction(displayName, Color.white, null);
            }
        }

        private static TeamPage CreateTeamPage(Team team)
        {
            if (!Teams.Any(x => x.Value == team))
                return null;

            Dictionary<string, Color> teamColors = new()
            {
                { "Infected", Color.green },
                { "Infected Children", new Color(0, 1, 0) },
                { "Survivors", Color.cyan },
                { "Unidentified", Color.gray },
            };

            string name = team != null ? team.DisplayName : "Unidentified";
            var color = teamColors.ContainsKey(name) ? teamColors[name] : Color.white;
            return new(ModPage.CreatePage($"{name} ({Teams.Count(x => x.Value == team)})", color), team);
        }

        private static void CreateErrorMessage(string error)
        {
            var label = ModPage.CreateFunction(error, Color.white, null);
            label.SetProperty(BoneLib.BoneMenu.ElementProperties.NoBorder);
        }
    }

    internal class TeamPage(Page page, Team team)
    {
        public Page Page { get; } = page;

        public Team Team { get; } = team;
    }
}