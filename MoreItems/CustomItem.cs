using System;
using System.Collections.Generic;
using System.Linq;
using SixModLoader.Api.Events.Player.Inventory;
using SixModLoader.Api.Extensions;

namespace MoreItems
{
    public abstract class CustomItem
    {
        public abstract string Name { get; }

        public static Dictionary<string, Type> Items { get; } = new Dictionary<string, Type>();

        public abstract Inventory.SyncItemInfo BaseItem { get; }

        public virtual Inventory.SyncItemInfo? Give(ReferenceHub player)
        {
            var weapon = player.weaponManager.weapons.FirstOrDefault(x => x.inventoryID == BaseItem.id);
            var oldCount = player.inventory.items.Count;
            player.inventory.AddNewItem(BaseItem.id, weapon != null && (int) BaseItem.durability == 0 ? weapon.maxAmmo : BaseItem.durability, BaseItem.modSight, BaseItem.modBarrel, BaseItem.modOther);
            var item = player.inventory.items.Count > oldCount ? player.inventory.items.Last() : (Inventory.SyncItemInfo?) null;

            if (item.HasValue)
            {
                MoreItemsMod.Instance.ItemManager.Items[item.Value.uniq] = this;
            }

            return item;
        }

        public virtual void SetStaticMessage(ReferenceHub player)
        {
            player.SetStaticMessage(0, Name.Color(player).Size(30));
        }

        public virtual void Equip(PlayerChangeItemEvent ev, bool equip)
        {
        }

        public virtual void Pickup(PlayerPickupItemEvent ev, Inventory.SyncItemInfo item)
        {
        }
    }
}