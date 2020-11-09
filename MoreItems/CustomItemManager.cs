using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mirror;
using Searching;
using SixModLoader;
using SixModLoader.Api.Events.Player;
using SixModLoader.Api.Events.Player.Class;
using SixModLoader.Api.Events.Player.Inventory;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using UnityEngine;
using Logger = SixModLoader.Logger;
using Priority = SixModLoader.Priority;

namespace MoreItems
{
    public class CustomItemManager
    {
        public MoreItemsMod Mod { get; }

        public CustomItemManager(MoreItemsMod mod)
        {
            Mod = mod;
            SixModLoader.SixModLoader.Instance.EventManager.Register(this);
        }

        internal readonly Dictionary<int, CustomItem> Items = new Dictionary<int, CustomItem>();
        internal readonly Dictionary<ReferenceHub, CustomItem> Held = new Dictionary<ReferenceHub, CustomItem>();
        public Dictionary<Pickup, CustomItem> Pickups { get; } = new Dictionary<Pickup, CustomItem>();

        [EventHandler]
        private void OnPlayerLeft(PlayerLeftEvent ev)
        {
            Held.Remove(ev.Player);
        }

        [EventHandler]
        internal void OnPlayerChangeItem(PlayerChangeItemEvent ev)
        {
            var oldCustomItem = ev.OldItem.GetCustomItem();
            if (oldCustomItem != null && Held.Remove(ev.Player))
            {
                ev.Player.SetStaticMessage(0, null);
                oldCustomItem.Equip(ev, false);
            }

            var customItem = ev.NewItem.GetCustomItem();
            Held[ev.Player] = customItem;
            customItem?.SetStaticMessage(ev.Player);
            customItem?.Equip(ev, true);
        }

        [EventHandler]
        [Priority(Priority.Lowest)]
        private void OnPlayerClassChange(PlayerRoleChangeEvent ev)
        {
            if (ev.Lite)
                return;

            if (Held.TryGetValue(ev.Player, out var customItem) && customItem != null && Held.Remove(ev.Player))
            {
                ev.Player.SetStaticMessage(0, null);
                customItem.Equip(new PlayerChangeItemEvent(ev.Player, new Inventory.SyncItemInfo { id = ItemType.None }, new Inventory.SyncItemInfo { id = ItemType.None }), false);
            }
        }

        [EventHandler]
        internal void OnItemDroppedEvent(PlayerDroppedItemEvent ev)
        {
            if (Items.ContainsKey(ev.Item.uniq))
            {
                var customItem = Items[ev.Item.uniq];
                if (ev.Player != null && ev.Player.gameObject != null)
                {
                    if (Items.Remove(ev.Item.uniq) && Held.TryGetValue(ev.Player, out var heldItem) && customItem == heldItem)
                    {
                        ev.Player.SetStaticMessage(0, null);
                        heldItem.Equip(new PlayerChangeItemEvent(ev.Player, ev.Item, new Inventory.SyncItemInfo { id = ItemType.None }), false);
                    }

                    ev.Player.SetStaticMessage(2, Mod.Translations.Dropped.Replace("{item}", customItem.Name.Color(ev.Player)).Size(30), 3);
                }

                Pickups[ev.Pickup] = customItem;
            }
        }

        [EventHandler]
        private void OnPickupItemEvent(PlayerPickupItemEvent ev)
        {
            var customItem = ev.Pickup.GetCustomItem();
            if (customItem != null)
            {
                ev.Cancelled = true;
                ev.Pickup.Delete();

                var item = customItem.Give(ev.Player);
                if (item == null)
                {
                    Logger.Error($"Couldn't give {customItem} after pickup!");
                    return;
                }

                customItem.Pickup(ev, item.Value);
                ev.Player.SetStaticMessage(2, Mod.Translations.PickedUp.Replace("{item}", customItem.Name.Color(ev.Player)).Size(30), 3);
            }
        }

        [HarmonyPatch(typeof(SyncList<Inventory.SyncItemInfo>), nameof(SyncList<Inventory.SyncItemInfo>.RemoveAt))]
        public static class RemoveAtPatch
        {
            public static void Prefix(Inventory.SyncListItemInfo __instance, int index)
            {
                try
                {
                    var itemInfo = __instance.objects[index];
                    InvokeRemovePatch(itemInfo);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(SyncList<Inventory.SyncItemInfo>), nameof(SyncList<Inventory.SyncItemInfo>.Remove))]
        public static class RemovePatch
        {
            public static void Prefix(Inventory.SyncListItemInfo __instance, Inventory.SyncItemInfo item)
            {
                try
                {
                    if (!__instance.objects.Contains(item))
                        return;

                    InvokeRemovePatch(item);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(SyncList<Inventory.SyncItemInfo>), nameof(SyncList<Inventory.SyncItemInfo>.Clear))]
        public static class ClearPatch
        {
            public static void Prefix(Inventory.SyncListItemInfo __instance)
            {
                try
                {
                    foreach (var itemInfo in __instance.objects)
                    {
                        InvokeRemovePatch(itemInfo);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private static void InvokeRemovePatch(Inventory.SyncItemInfo itemInfo)
        {
            var customItem = itemInfo.GetCustomItem();
            if (customItem == null)
                return;

            var referenceHub = MoreItemsMod.Instance.ItemManager.Held.SingleOrDefault(x => x.Value == customItem).Key;
            if (referenceHub == null)
                return;

            Logger.Debug($"RemovePatch - {referenceHub.characterClassManager.UserId} removed {customItem}");

            MoreItemsMod.Instance.ItemManager.OnPlayerChangeItem(new PlayerChangeItemEvent
            (
                referenceHub,
                itemInfo,
                new Inventory.SyncItemInfo { id = ItemType.None }
            ));
        }

        [HarmonyPatch(typeof(PlayerMovementSync), nameof(PlayerMovementSync.ReceiveRotation), typeof(Vector2))]
        public static class CallCmdSendRotationsPatch
        {
            private static int PickupLayer { get; } = LayerMask.NameToLayer("Pickup");
            private static Dictionary<ReferenceHub, DateTimeOffset> Cooldown { get; } = new Dictionary<ReferenceHub, DateTimeOffset>();
            private static Dictionary<ReferenceHub, Pickup> Current { get; } = new Dictionary<ReferenceHub, Pickup>();

            public static void Postfix(PlayerMovementSync __instance)
            {
                try
                {
                    var player = __instance._hub;
                    if (Cooldown.ContainsKey(player) && DateTimeOffset.Now - Cooldown[player] < TimeSpan.FromSeconds(0.25))
                        return;

                    Cooldown[player] = DateTimeOffset.Now;
                    var searching = player.GetComponent<SearchCoordinator>();

                    if (Physics.Raycast(player.PlayerCameraReference.position, player.PlayerCameraReference.forward, out var hit, searching.rayDistance, 1 << PickupLayer) && hit.collider?.gameObject?.transform?.parent?.gameObject != null)
                    {
                        var pickup = hit.collider.gameObject.GetComponentInParent<Pickup>();
                        if (pickup != null && (!Current.TryGetValue(player, out var currentPickup) || pickup != currentPickup))
                        {
                            var customItem = pickup.GetCustomItem();

                            if (customItem != null)
                            {
                                Current[player] = pickup;
                                player.SetStaticMessage(2, MoreItemsMod.Instance.Translations.LookingAt.Replace("{item}", customItem.Name.Color(player)).Size(30), 3);
                            }
                        }
                    }
                    else
                    {
                        if (Current.ContainsKey(player))
                        {
                            Current.Remove(player);
                            player.SetStaticMessage(2, null);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }

    public static class Extensions
    {
        public static CustomItem GetCustomItem(this Inventory.SyncItemInfo item)
        {
            var items = MoreItemsMod.Instance.ItemManager.Items;
            return items.ContainsKey(item.uniq) ? items[item.uniq] : null;
        }

        public static CustomItem GetCustomItem(this Inventory inventory)
        {
            var items = MoreItemsMod.Instance.ItemManager.Items;
            return items.ContainsKey(inventory.itemUniq) ? items[inventory.itemUniq] : null;
        }

        public static CustomItem GetCustomItem(this Pickup pickup)
        {
            var pickups = MoreItemsMod.Instance.ItemManager.Pickups;
            return pickups.ContainsKey(pickup) ? pickups[pickup] : null;
        }
    }
}