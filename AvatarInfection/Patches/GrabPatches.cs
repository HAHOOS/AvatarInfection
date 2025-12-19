using System.Collections.Generic;

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

using UnityEngine;

namespace AvatarInfection.Patches
{
    public static class GrabPatches
    {
        internal readonly static Dictionary<Grip, float> HoldTime = [];

        #region Patches

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grip), nameof(Grip.OnAttachedToHand))]
        public static void Postfix(Grip __instance, Hand hand)
            => Grabbed(__instance, hand);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grip), nameof(Grip.OnDetachedFromHand))]
        public static void Postfix2(Grip __instance)
        {
            if (HoldTime.ContainsKey(__instance))
                HoldTime.Remove(__instance);
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


        #endregion

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
                RegisterTouch(grip, otherPlayer.PlayerID.PlatformID);
        }

        private static void RegisterTouch(Grip grip, ulong longId)
        {
            if (Infection.Instance.Config.HoldTime.Value == 0)
                EventManager.TryInvokeEvent(Infection.EventType.PlayerInfected, longId);
            else
                HoldTime.Add(grip, 0);
        }

        private static bool CanGrab(Hand hand, GameObject gameObject)
        {
            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance?.IsStarted != true)
                return true;

            if (hand == null || gameObject == null || Player.RigManager != hand?.GetComponentInParent<RigManager>())
                return true;

            TeamMetadata config = GetMetadata();

            if (config == null)
                return true;

            var gun = gameObject.GetComponent<Gun>() ?? gameObject.GetComponentInParent<Gun>();

            if (IsSpawnGun(gameObject))
                return true;

            return gun == null || config.CanUseGuns.ClientValue;
        }

        private static TeamMetadata GetMetadata()
        {
            if (Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.Infected)
            {
                return Infection.Instance.Infected.Metadata;
            }
            else if (Infection.Instance.TeamManager.GetLocalTeam() == Infection.Instance.InfectedChildren)
            {
                if (!Infection.Instance.Config.AddInfectedChildrenTeam.Value)
                    return Infection.Instance.Infected.Metadata;
                else
                    return Infection.Instance.InfectedChildren.Metadata;
            }
            else
            {
                return Infection.Instance.Survivors.Metadata;
            }
        }

        private static bool IsSpawnGun(GameObject gameObject)
        {
            var spawnGun = gameObject.GetComponent<SpawnGun>() ?? gameObject.GetComponentInParent<SpawnGun>();
            return spawnGun != null;
        }

        internal static void Update()
        {
            ClearHoldIfNecessary();

            foreach (var hold in new Dictionary<Grip, float>(HoldTime))
            {
                var grip = hold.Key;
                HoldTime[grip] = hold.Value + TimeUtilities.DeltaTime;
                if (HoldTime[grip] >= Infection.Instance.Config.HoldTime.Value)
                {
                    if (!grip._marrowEntity)
                        continue;

                    if (!NetworkPlayerManager.TryGetPlayer(grip._marrowEntity, out var player))
                        continue;

                    if (Infection.Instance.IsPlayerInfected(player.PlayerID))
                        continue;

                    HoldTime.Remove(grip);
                    EventManager.TryInvokeEvent(Infection.EventType.PlayerInfected, player.PlayerID.PlatformID);
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
}