using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Managers;
using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Interaction;
using Il2CppSLZ.Marrow;

using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;

using UnityEngine;

namespace AvatarInfection.Patches
{
    public static class GrabPatches
    {
        internal readonly static List<GripData> HoldTime = [];

        #region Patches

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grip), nameof(Grip.OnAttachedToHand))]
        public static void Postfix(Grip __instance, Hand hand)
            => Grabbed(__instance, hand);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grip), nameof(Grip.OnDetachedFromHand))]
        public static void Postfix2(Grip __instance)
        {
            if (Has(__instance))
                HoldTime.RemoveAll(x => x.Grip == __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hand), nameof(Hand.AttachObject))]
        [HarmonyPriority(10000)]
        public static bool GrabAttempt(Hand __instance, GameObject objectToAttach)
            => CanGrab(__instance, objectToAttach);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandHoverBegin))]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandHoverEnd))]
        [HarmonyPriority(10000)]
        public static bool InventoryGrabAttempt(InventorySlotReceiver __instance, Hand hand)
            => CanGrab(hand, __instance._weaponHost?.GetHostGameObject());

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandGrab))]
        [HarmonyPriority(10000)]
        public static bool InventoryGrabAttempt2(InventorySlotReceiver __instance, Hand hand)
        {
            bool res = CanGrab(hand, __instance._weaponHost?.GetHostGameObject());
            if (!res)
                __instance.DropWeapon();

            return res;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractableIcon), nameof(InteractableIcon.MyFarHandHoverBegin))]
        [HarmonyPatch(typeof(InteractableIcon), nameof(InteractableIcon.MyHandHoverBegin))]
        [HarmonyPriority(10000)]
        public static bool IconAttempt(InteractableIcon __instance, Hand hand)
            => CanGrab(hand, __instance.gameObject);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.CoPull))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnStartAttach))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverBegin))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverUpdate))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverEnd))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnForcePullComplete))]
        [HarmonyPriority(10000)]
        public static bool ForceGrabAttempt(Hand hand, ForcePullGrip __instance)
            => CanGrab(hand, __instance?.gameObject);

        #endregion Patches

        private static bool Has(Grip grip)
            => HoldTime.Any(x => x.Grip == grip);

        private static void Grabbed(Grip grip, Hand hand)
        {
            if (!NetworkInfo.IsHost)
                return;

            if (Infection.Instance == null)
                return;

            if (!Infection.Instance.IsStarted ||
                Infection.Instance.Config.InfectType.Value != Infection.InfectType.TOUCH ||
                !Infection.Instance.InfectedLooking.GetValue())
            {
                return;
            }

            var plrEntity = hand.manager?.physicsRig?.marrowEntity;

            if (plrEntity == null)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(plrEntity, out var player))
                return;

            if (Infection.Instance.TeamManager.GetPlayerTeam(player.PlayerID) == Infection.Instance.Survivors)
                return;

            if (!grip._marrowEntity)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(grip._marrowEntity, out var otherPlayer))
                return;

            if (Infection.Instance.TeamManager.GetPlayerTeam(otherPlayer.PlayerID) == Infection.Instance.Survivors)
                RegisterTouch(grip, otherPlayer.PlayerID.PlatformID, player.PlayerID.PlatformID);
        }

        private static void RegisterTouch(Grip grip, ulong longId, ulong by)
        {
            if (Infection.Instance.Config.HoldTime.Value == 0)
                EventManager.TryInvokeEvent(Infection.EventType.PlayerInfected, longId);
            else
                HoldTime.Add(new(grip, by));
        }

        private static bool CanGrab(Hand hand, GameObject gameObject)
        {
            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance?.IsStarted != true)
                return true;

            if (hand == null || gameObject == null || Player.RigManager != hand?.GetComponentInParent<RigManager>())
                return true;

            TeamMetadata config = Infection.Instance.TeamManager.GetLocalTeam().Metadata;

            if (config == null)
                return true;

            var gun = gameObject.GetComponent<Gun>() ?? gameObject.GetComponentInParent<Gun>();

            if (IsSpawnGun(gameObject))
                return true;

            return gun == null || config.CanUseGuns.Value;
        }

        private static bool IsSpawnGun(GameObject gameObject)
        {
            var spawnGun = gameObject.GetComponent<SpawnGun>() ?? gameObject.GetComponentInParent<SpawnGun>();
            return spawnGun != null;
        }

        internal static void Update()
        {
            ClearHoldIfNecessary();

            List<GripData> list = [.. HoldTime];
            for (int i = 0; i < list.Count; i++)
            {
                GripData hold = list[i];
                hold.Time += TimeUtilities.DeltaTime;
                if (hold.Time >= Infection.Instance.Config.HoldTime.Value)
                {
                    if (!hold.Grip._marrowEntity)
                        continue;

                    if (!NetworkPlayerManager.TryGetPlayer(hold.Grip._marrowEntity, out var player))
                        continue;

                    if (Infection.Instance.IsPlayerInfected(player.PlayerID))
                        continue;

                    HoldTime.RemoveAll(x => x.Grip == hold.Grip);
                    EventManager.TryInvokeEvent(Infection.EventType.PlayerInfected, new PlayerInfectedData(player.PlayerID.PlatformID, (long)hold.By));
                }
            }
        }

        private static void ClearHoldIfNecessary()
        {
            if (NetworkInfo.HasServer)
                return;

            if (!NetworkInfo.IsHost)
                return;

            if (Infection.Instance?.IsStarted == true)
                return;

            if (Player.RigManager != null)
                return;

            HoldTime.Clear();
        }
    }

    public class GripData(Grip grip, ulong by, float time = 0)
    {
        public Grip Grip { get; set; } = grip;

        public ulong By { get; set; } = by;

        public float Time { get; set; } = time;
    }
}