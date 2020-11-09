using System;
using System.Linq;
using HarmonyLib;
using MoreItems;
using MoreWeapons.Grenades;
using MoreWeapons.Weapons;
using SixModLoader.Api.Configuration;
using SixModLoader.Api.Extensions;
using SixModLoader.Mods;
using UnityEngine;
using Logger = SixModLoader.Logger;

namespace MoreWeapons
{
    [Mod("pl.js6pak.MoreWeaponsMod")]
    public class MoreWeaponsMod
    {
        public static MoreWeaponsMod Instance { get; private set; }

        [AutoConfiguration(ConfigurationType.Configuration)]
        public Configuration Configuration { get; set; }

        [AutoConfiguration(ConfigurationType.Translations)]
        public Translations Translations { get; set; }

        [AutoHarmony]
        public Harmony Harmony { get; set; }

        public MoreWeaponsMod()
        {
            Instance = this;
            ConfigurationManager.Converters.Add(new SyncItemInfoConverter());

            CustomItem.Items["sniper_rifle"] = typeof(SniperRifle);
            CustomItem.Items["shotgun"] = typeof(Shotgun);
            CustomItem.Items["taser"] = typeof(Taser);
            CustomItem.Items["medi_gun"] = typeof(MediGun);
            CustomItem.Items["grenade_launcher"] = typeof(GrenadeLauncher);

            CustomItem.Items["heal_grenade"] = typeof(HealGrenade);
            CustomItem.Items["molotov"] = typeof(Molotov);
        }

        [HarmonyPatch(typeof(HostItemSpawner), nameof(HostItemSpawner.SetPos))]
        public static class SetPosPatch
        {
            public static void Postfix(Pickup pickup, Vector3 pos, ItemType item, Vector3 rot)
            {
                try
                {
                    CustomItem customItem = null;

                    var position = RandomItemSpawner.singleton.posIds.Single(x => x.posID == "LC_Armory").position;
                    if (pos == position.position)
                    {
                        customItem = new Shotgun();
                    }

                    position = RandomItemSpawner.singleton.posIds.Where(x => x.posID == "SFA_TCross").ElementAt(3).position;
                    if (pos == position.position)
                    {
                        customItem = new SniperRifle();
                    }

                    if (customItem != null)
                    {
                        pickup.ItemId = customItem.BaseItem.id;
                        pickup.RefreshDurability(true, true);
                        MoreItemsMod.Instance.ItemManager.Pickups[pickup] = customItem;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}