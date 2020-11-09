using SixModLoader.Api.Events.Player.Weapon;
using SixModLoader.Events;

namespace MoreItems.Weapons
{
    public class CustomWeaponManager
    {
        public MoreItemsMod Mod { get; }

        public CustomWeaponManager(MoreItemsMod mod)
        {
            Mod = mod;
            SixModLoader.SixModLoader.Instance.EventManager.Register(this);
        }

        [EventHandler]
        private void OnPlayerWeaponReload(PlayerWeaponReloadEvent ev)
        {
            if (!ev.Cancelled)
            {
                var customWeapon = ev.Player.inventory.GetCustomWeapon();
                customWeapon?.Reload(ev);
            }
        }

        [EventHandler]
        private void OnLateShootEvent(PlayerShotByPlayerEvent ev)
        {
            if (!ev.Cancelled)
            {
                var customWeapon = ev.Shooter.inventory.GetCustomWeapon();
                customWeapon?.Shot(ev);
            }
        }

        [EventHandler]
        private void OnShootEvent(PlayerShootEvent ev)
        {
            if (!ev.Cancelled)
            {
                var customWeapon = ev.Player.inventory.GetCustomWeapon();
                customWeapon?.Shoot(ev);
            }
        }

        [EventHandler]
        private void OnChangeModPreferencesEvent(PlayerWeaponChangeAttachmentsEvent ev)
        {
            if (!ev.Cancelled)
            {
                var customWeapon = ev.Player.inventory.GetCustomWeapon();
                if (customWeapon != null)
                {
                    ev.Cancelled = true;
                }
            }
        }
    }

    public static class Extensions
    {
        public static CustomWeapon GetCustomWeapon(this Inventory.SyncItemInfo item)
        {
            return item.GetCustomItem() as CustomWeapon;
        }

        public static CustomWeapon GetCustomWeapon(this Inventory inventory)
        {
            return inventory.GetCustomItem() as CustomWeapon;
        }

        public static CustomWeapon GetCustomWeapon(this Pickup pickup)
        {
            return pickup.GetCustomItem() as CustomWeapon;
        }
    }
}