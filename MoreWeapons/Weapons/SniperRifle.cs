using SixModLoader.Api.Events.Player.Weapon;

namespace MoreWeapons.Weapons
{
    public class SniperRifleConfiguration : WeaponConfiguration
    {
        public RoledDamageMultiplier DamageMultiplier { get; set; } = new RoledDamageMultiplier();

        public SniperRifleConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GunE11SR, modSight = 4 };
            FireRate = 2f;
            MaxAmmo = 10;
        }

        public class RoledDamageMultiplier
        {
            public float Scp { get; set; } = 10;
            public float Human { get; set; } = 5;
        }
    }

    public class SniperRifle : ConfiguratedCustomWeapon<SniperRifleConfiguration>
    {
        public override SniperRifleConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.SniperRifle;
        public override string Name => MoreWeaponsMod.Instance.Translations.SniperRifle;

        public override void Shot(PlayerShotByPlayerEvent ev)
        {
            base.Shot(ev);

            ev.Damage *= ev.Player.characterClassManager.IsHuman() ? Configuration.DamageMultiplier.Human : Configuration.DamageMultiplier.Scp;
        }
    }
}