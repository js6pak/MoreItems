using System;
using System.Collections.Generic;
using System.Linq;
using Grenades;
using HarmonyLib;
using MEC;
using Mirror;
using SixModLoader.Api.Events.Player.Inventory;
using SixModLoader.Api.Events.Player.Weapon;
using UnityEngine;
using Logger = SixModLoader.Logger;
using Object = UnityEngine.Object;

namespace MoreWeapons.Weapons
{
    public class GrenadeLauncherConfiguration : WeaponConfiguration
    {
        public GrenadeLauncherConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GunMP7 };
            FireRate = 1f;
            MaxAmmo = 4;
        }
    }

    public class GrenadeLauncher : ConfiguratedCustomWeapon<GrenadeLauncherConfiguration>
    {
        public override GrenadeLauncherConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.GrenadeLauncher;
        public override string Name => MoreWeaponsMod.Instance.Translations.GrenadeLauncher;

        public override void Shoot(PlayerShootEvent ev)
        {
            base.Shoot(ev);
            if (ev.Cancelled)
                return;
            ev.Cancelled = true;

            var grenadeManager = ev.Player.GetComponent<GrenadeManager>();

            var component = Object.Instantiate(grenadeManager.availableGrenades[0].grenadeInstance).GetComponent<Grenade>();
            component.InitData(grenadeManager, Vector3.zero, ev.Player.PlayerCameraReference.forward, 1);
            NetworkServer.Spawn(component.gameObject);

            ev.Player.inventory.items.ModifyDuration(ev.Player.inventory.GetItemIndex(), 0);
        }

        public override void Reload(PlayerWeaponReloadEvent ev)
        {
            ev.Cancelled = true;
            if (ev.AnimationOnly) return;
            foreach (var item in ev.Player.inventory.items.Where(x => x.id == ItemType.GrenadeFrag).ToList().TakeWhile(item => Ammo < MaxAmmo))
            {
                Ammo++;
                ev.Player.inventory.items.Remove(item);
            }

            SetFakeAmmo(ev.Player);
            SetStaticMessage(ev.Player);
        }

        private static Dictionary<AmmoBox, SyncListUInt> LastAmmobox { get; } = new Dictionary<AmmoBox, SyncListUInt>();

        public override void Equip(PlayerChangeItemEvent ev, bool equip)
        {
            base.Equip(ev, equip);
            var ammoBox = ev.Player.ammoBox;
            if (equip)
            {
                LastAmmobox[ammoBox] = ammoBox.amount;

                SetNetworkAmountPatch.Disable = true;
                ammoBox[1] = 1;
                SetNetworkAmountPatch.Disable = false;

                Timing.CallDelayed(ammoBox.syncInterval, () => { ammoBox.amount[1] = LastAmmobox[ammoBox][1]; });
            }
            else if (LastAmmobox.TryGetValue(ammoBox, out var ammo))
            {
                LastAmmobox.Remove(ammoBox);
                ammoBox[1] = ammo[1];
            }
        }

        [HarmonyPatch(typeof(AmmoBox), "Item", MethodType.Setter)]
        public static class SetNetworkAmountPatch
        {
            internal static bool Disable;

            public static void Prefix(AmmoBox __instance, int type, uint value)
            {
                if (Disable || type != 1)
                    return;

                try
                {
                    if (LastAmmobox.TryGetValue(__instance, out var ammo))
                    {
                        ammo[1] = ammo[1] + value;
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