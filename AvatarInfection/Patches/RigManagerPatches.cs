﻿using System;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow;

using Il2CppCysharp.Threading.Tasks;

using LabFusion.Network;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;

using HarmonyLib;
using AvatarInfection.Utilities;

namespace AvatarInfection.Patches
{
    [HarmonyPatch(typeof(RigManager))]
    public static class RigManagerPatches
    {
        internal static bool Ignore = false;

        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)]
        [HarmonyPatch(nameof(RigManager.SwapAvatarCrate))]
        public static bool AvatarChangeOverride(RigManager __instance, Barcode barcode, ref UniTask<bool> __result)
        {
            try
            {
                if (barcode == null || string.IsNullOrWhiteSpace(barcode.ID) || barcode.ID == Barcode.EMPTY)
                    return true;

                if (__instance == null)
                    return true;

                if (!NetworkInfo.HasServer)
                    return true;

                if (__instance?.IsLocalPlayer() != true)
                    return true;

                if (!GamemodeManager.IsGamemodeStarted)
                    return true;

                if (GamemodeManager.ActiveGamemode == null)
                    return true;

                if (GamemodeManager.ActiveGamemode.Barcode != Infection.Defaults.Barcode)
                    return true;

                if (Infection.TeamManager == null)
                    return true;

                if (Infection.TeamManager.GetLocalTeam() == null)
                    return true;

                if (string.IsNullOrWhiteSpace(Infection.SelectedAvatar) || new Barcode(Infection.SelectedAvatar) == null)
                    return true;

                if (barcode == new Barcode(Infection.SelectedAvatar))
                {
                    if (Ignore)
                    {
                        Ignore = false;
                        return true;
                    }
                    else
                    {
                        if (Infection.TeamManager.GetLocalTeam() == Infection.Infected)
                        {
                            __result = new UniTask<bool>(true);
                            Ignore = true;
                            FusionPlayerExtended.SetAvatarOverride(Infection.SelectedAvatar);
                            return false;
                        }
                        else
                        {
                            __result = new UniTask<bool>(true);
                            return false;
                        }
                    }
                }

                bool returned = Infection.TeamManager.GetLocalTeam() == Infection.UnInfected;

                if (!returned) __result = new UniTask<bool>(true);

                return returned;
            }
            catch (Exception e)
            {
                FusionModule.Logger.Error($"An unexpected error has occurred while handing SwapAvatarCrate in RigManager, exception:\n{e}");
                return true;
            }
        }
    }
}