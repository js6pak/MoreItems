using System.ComponentModel;
using MoreItems.Weapons;
using MoreWeapons.Grenades;
using MoreWeapons.Weapons;

namespace MoreWeapons
{
    public class Configuration
    {
        public GrenadeLauncherConfiguration GrenadeLauncher { get; set; } = new GrenadeLauncherConfiguration();
        public MediGunConfiguration MediGun { get; set; } = new MediGunConfiguration();
        public ShotgunConfiguration Shotgun { get; set; } = new ShotgunConfiguration();
        public SniperRifleConfiguration SniperRifle { get; set; } = new SniperRifleConfiguration();
        public TaserConfiguration Taser { get; set; } = new TaserConfiguration();

        public HealGrenadeConfiguration HealGrenade { get; set; } = new HealGrenadeConfiguration();
        public MolotovConfiguration Molotov { get; set; } = new MolotovConfiguration();
    }

    public abstract class ItemConfiguration
    {
        [Description("Vanilla model for this item")]
        public Inventory.SyncItemInfo BaseItem { get; set; }
    }

    public abstract class WeaponConfiguration : ItemConfiguration
    {
        [Description("Fire rate of weapon in seconds (can't be lower than vanilla model)")]
        public float FireRate { get; set; }

        [Description("Max ammo of weapon")]
        public int MaxAmmo { get; set; }
    }

    public abstract class ConfiguratedCustomWeapon<TConfiguration> : CustomWeapon where TConfiguration : WeaponConfiguration
    {
        public abstract TConfiguration Configuration { get; }

        public override Inventory.SyncItemInfo BaseItem => Configuration.BaseItem;
        public override float FireRate => Configuration.FireRate;
        public override int MaxAmmo => Configuration.MaxAmmo;
    }
}