using System.Collections.Generic;

using AvatarInfection.Utilities;

using HarmonyLib;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Interaction;
using Il2CppSLZ.Marrow;

using LabFusion.Entities;
using LabFusion.Extensions;
using LabFusion.Network;

using UnityEngine;

namespace AvatarInfection.Patches
{
    public static class GrabPatches
    {
        internal static Dictionary<Grip, float> HoldTime = [];

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

        private static void Grabbed(Grip grip, Hand hand)
        {
            if (!NetworkInfo.IsServer)
                return;

            if (Infection.Instance == null)
                return;

            if (!Infection.Instance.IsStarted ||
                Infection.InfectType != Infection.InfectTypeEnum.TOUCH ||
                !Infection.Instance.InfectedLooking.GetValue())
            {
                return;
            }

            var plrEntity = hand.manager?.physicsRig?.marrowEntity;

            if (plrEntity == null)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(plrEntity, out var player))
                return;

            if (Infection.TeamManager.GetPlayerTeam(player.PlayerId) == Infection.UnInfected)
                return;

            if (!grip._marrowEntity)
                return;

            if (!NetworkPlayerManager.TryGetPlayer(grip._marrowEntity, out var otherPlayer))
                return;

            if (Infection.TeamManager.GetPlayerTeam(otherPlayer.PlayerId) == Infection.UnInfected)
            {
                var longId = otherPlayer.PlayerId.LongId;

                if (Infection.Instance.HoldTime == 0)
                    Infection.Instance.InfectEvent.TryInvoke(longId.ToString());
                else
                    HoldTime.Add(grip, 0);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hand), nameof(Hand.AttachObject))]
        [HarmonyPatch(typeof(Hand), nameof(Hand.AttachJoint))]
        [HarmonyPatch(typeof(Hand), nameof(Hand.AttachIgnoreBodyJoints))]
        [HarmonyPatch(typeof(Hand), nameof(Hand.PrepareJoint))]
        [HarmonyPriority(10000)]
        public static bool GrabAttempt(Hand __instance, GameObject objectToAttach)
            => CanGrab(__instance, objectToAttach);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandHoverBegin))]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandHoverEnd))]
        [HarmonyPriority(10000)]
        public static bool InventoryGrabAttempt(InventorySlotReceiver __instance, Hand hand)
        {
            var weapon = __instance._weaponHost?.GetHostGameObject();
            if (weapon == null)
                return true;

            return CanGrab(hand, weapon);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandGrab))]
        [HarmonyPriority(10000)]
        public static bool InventoryGrabAttempt2(InventorySlotReceiver __instance, Hand hand)
        {
            var weapon = __instance._weaponHost?.GetHostGameObject();
            if (weapon == null)
                return true;

            bool res = CanGrab(hand, weapon);
            if (!res)
                __instance.DropWeapon();

            return res;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractableIcon), nameof(InteractableIcon.MyFarHandHoverBegin))]
        [HarmonyPatch(typeof(InteractableIcon), nameof(InteractableIcon.MyHandHoverBegin))]
        [HarmonyPriority(10000)]
        public static bool IconAttempt(InteractableIcon __instance, Hand hand)
        {
            var gameObject = __instance.gameObject;
            if (gameObject == null || hand == null)
                return true;

            return CanGrab(hand, gameObject);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.CoPull))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnStartAttach))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverBegin))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverUpdate))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnFarHandHoverEnd))]
        [HarmonyPatch(typeof(ForcePullGrip), nameof(ForcePullGrip.OnForcePullComplete))]
        [HarmonyPriority(10000)]
        public static bool ForceGrabAttempt(Hand hand, ForcePullGrip __instance)
        {
            var gameObject = __instance?.gameObject;

            if (gameObject == null)
                return true;

            return CanGrab(hand, gameObject);
        }

        private static bool CanGrab(Hand hand, GameObject gameObject)
        {
            if (!NetworkInfo.HasServer)
                return true;

            if (Infection.Instance == null)
                return true;

            if (!Infection.Instance.IsStarted)
                return true;

            if (gameObject == null || hand == null)
                return true;

            if (!hand.IsPartOfPlayer() || !hand.IsPartOfSelf())
                return true;

            var config = Infection.TeamManager.GetLocalTeam() == Infection.Infected ? Infection.InfectedMetadata : Infection.UnInfectedMetadata;
            if (config == null)
                return true;

            var gun = gameObject.GetComponent<Gun>() ?? gameObject.GetComponentInParent<Gun>();
            var spawnGun = gameObject.GetComponent<SpawnGun>() ?? gameObject.GetComponentInParent<SpawnGun>();

            if (spawnGun != null)
                return true;

            return gun == null || config.Config.CanUseGuns;
        }

        internal static void Update()
        {
            if (!NetworkInfo.HasServer || !NetworkInfo.IsServer)
            {
                HoldTime.Clear();
                return;
            }

            if (Infection.Instance?.IsStarted != true)
            {
                HoldTime.Clear();
                return;
            }

            var copy = new Dictionary<Grip, float>(HoldTime);

            foreach (var hold in copy)
            {
                var grip = hold.Key;
                HoldTime[grip] = hold.Value + TimeUtilities.DeltaTime;
                if (HoldTime[grip] >= Infection.Instance.HoldTime)
                {
                    if (!grip._marrowEntity)
                        continue;

                    if (!NetworkPlayerManager.TryGetPlayer(grip._marrowEntity, out var player))
                        continue;

                    if (Infection.TeamManager.GetPlayerTeam(player.PlayerId) == Infection.Infected)
                        continue;

                    HoldTime.Remove(grip);
                    Infection.Instance.InfectEvent.TryInvoke(player.PlayerId.LongId.ToString());
                }
            }
        }
    }
}