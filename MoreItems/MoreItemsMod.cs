using HarmonyLib;
using MoreItems.Grenades;
using MoreItems.Weapons;
using SixModLoader.Api.Configuration;
using SixModLoader.Api.Extensions;
using SixModLoader.Mods;

namespace MoreItems
{
    [Mod("pl.js6pak.MoreItems")]
    public class MoreItemsMod
    {
        public static MoreItemsMod Instance { get; private set; }

        [AutoHarmony]
        public Harmony Harmony { get; set; }

        [AutoConfiguration(ConfigurationType.Translations)]
        public Translations Translations { get; set; }

        public CustomItemManager ItemManager { get; }
        public CustomWeaponManager WeaponManager { get; }
        public CustomGrenadeManager GrenadeManager { get; }

        public MoreItemsMod()
        {
            Instance = this;
            ItemManager = new CustomItemManager(this);
            WeaponManager = new CustomWeaponManager(this);
            GrenadeManager = new CustomGrenadeManager(this);

            ConfigurationManager.Converters.Add(new BaseItemTypeConverter());
        }
    }
}