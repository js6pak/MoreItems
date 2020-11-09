using CustomPlayerEffects;

namespace MoreWeapons.Grenades
{
    public class MolotovConfiguration : ItemConfiguration
    {
        public MolotovConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GrenadeFrag };
        }
    }

    public class Molotov : CustomFragGrenade
    {
        public MolotovConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.Molotov;
        public override string Name => MoreWeaponsMod.Instance.Translations.Molotov;
        public override Inventory.SyncItemInfo BaseItem => Configuration.BaseItem;

        public override void Explode(ReferenceHub player)
        {
            player.playerEffectsController.EnableEffect<Burned>(12, true);
            player.playerEffectsController.EnableEffect<Bleeding>(12, true);
        }
    }
}