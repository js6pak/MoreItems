using System;
using System.ComponentModel;
using SixModLoader.Api.Events.Player.Weapon;

namespace MoreWeapons.Weapons
{
    public class MediGunConfiguration : WeaponConfiguration
    {
        [Description("Max healable health = class max health * this (set to 1 to disable)")]
        public float OverMaxHealth { get; set; } = 1.5f;

        public float HealAmount { get; set; } = 10f;

        public MediGunConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GunUSP };
            FireRate = 1f;
            MaxAmmo = 18;
        }
    }

    public class MediGun : ConfiguratedCustomWeapon<MediGunConfiguration>
    {
        public override MediGunConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.MediGun;
        public override string Name => MoreWeaponsMod.Instance.Translations.MediGun;

        public override void Shot(PlayerShotByPlayerEvent ev)
        {
            base.Shot(ev);

            if (ev.Player.characterClassManager.CurRole.team != ev.Shooter.characterClassManager.CurRole.team)
            {
                ev.Cancelled = false;
            }
            else
            {
                ev.Cancelled = true;
                ev.Player.playerStats.Health = Math.Min(
                    ev.Player.playerStats.maxHP * Configuration.OverMaxHealth,
                    ev.Player.playerStats.Health + Configuration.HealAmount
                );
                ev.Shooter.weaponManager.RpcConfirmShot(true, ev.Shooter.weaponManager.curWeapon);
            }
        }
    }
}