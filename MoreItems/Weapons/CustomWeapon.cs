using System.Linq;
using MEC;
using SixModLoader.Api.Events.Player.Inventory;
using SixModLoader.Api.Events.Player.Weapon;
using SixModLoader.Api.Extensions;

namespace MoreItems.Weapons
{
    public abstract class CustomWeapon : CustomItem
    {
        protected CustomWeapon()
        {
            Ammo = MaxAmmo;
        }

        /// <summary>
        /// Timeout in seconds (can't be faster than base weapon)
        /// </summary>
        public virtual float FireRate { get; } = 1;

        public virtual int MaxAmmo { get; } = 8;
        public virtual int Ammo { get; set; }

        public override void SetStaticMessage(ReferenceHub player)
        {
            player.SetStaticMessage(0, $"{Name.Color(player)} - {(Ammo <= 0 ? Ammo.ToString().Color("red") : Ammo.ToString())}/{MaxAmmo} ammo".Size(30));
        }

        public virtual void SetFakeAmmo(ReferenceHub player, Inventory.SyncItemInfo? item = null)
        {
            item ??= player.inventory.GetItemInHand();
            var weaponManager = player.weaponManager;
            var weapon = weaponManager.weapons.FirstOrDefault(x => x.inventoryID == item.Value.id);

            if (weapon != null)
            {
                player.inventory.items.ModifyDuration(player.inventory.items.FindIndex(x => x.uniq == item.Value.uniq), (float) Ammo / MaxAmmo * weapon.maxAmmo);
            }
        }

        public virtual void Shoot(PlayerShootEvent ev)
        {
            if (Ammo <= 0)
            {
                ev.Cancelled = true;
                return;
            }

            Ammo--;
            SetStaticMessage(ev.Player);

            var shooter = ev.Player;
            shooter.inventory.items.ModifyDuration(shooter.inventory.GetItemIndex(), 1);
            var item = shooter.inventory.GetItemInHand();
            Timing.CallDelayed(FireRate, () => SetFakeAmmo(shooter, item));
        }

        public virtual void Shot(PlayerShotByPlayerEvent ev)
        {
        }

        public virtual void Reload(PlayerWeaponReloadEvent ev)
        {
            var weaponManager = ev.Player.weaponManager;
            if (!ev.AnimationOnly)
            {
                ev.Cancelled = true;

                weaponManager._reloadingWeapon = -100;
                var ammoType = weaponManager.weapons[weaponManager.curWeapon].ammoType;

                var ammoBox = weaponManager._hub.ammoBox[ammoType];
                while (ammoBox > 0 && Ammo < MaxAmmo)
                {
                    ammoBox--;
                    Ammo++;
                }

                SetFakeAmmo(ev.Player);
                weaponManager._hub.ammoBox[ammoType] = ammoBox;
                SetStaticMessage(ev.Player);
            }
        }

        public override void Pickup(PlayerPickupItemEvent ev, Inventory.SyncItemInfo item)
        {
            SetFakeAmmo(ev.Player, item);
        }
    }
}