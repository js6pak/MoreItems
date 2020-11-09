namespace MoreWeapons.Grenades
{
    public class HealGrenadeConfiguration : ItemConfiguration
    {
        public float HealAmount { get; set; } = 50f;

        public HealGrenadeConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GrenadeFlash };
        }
    }

    public class HealGrenade : CustomFragGrenade
    {
        public HealGrenadeConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.HealGrenade;
        public override string Name => MoreWeaponsMod.Instance.Translations.HealGrenade;
        public override Inventory.SyncItemInfo BaseItem => Configuration.BaseItem;

        public override void Explode(ReferenceHub player)
        {
            player.playerStats.HealHPAmount(Configuration.HealAmount);
        }
    }
}