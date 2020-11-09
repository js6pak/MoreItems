using System.ComponentModel;
using HarmonyLib;
using Security;
using SixModLoader.Api.Events.Player.Weapon;
using UnityEngine;
using Logger = SixModLoader.Logger;
using Random = UnityEngine.Random;

namespace MoreWeapons.Weapons
{
    public class ShotgunConfiguration : WeaponConfiguration
    {
        public float DamageMultiplier { get; set; } = 1.5f;
        public int Shells { get; set; } = 8;
        public int SoundShells { get; set; } = 4;

        [Description("Magic number, don't ask me")]
        public double Accuracy { get; set; } = 0.1;

        public ShotgunConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GunMP7 };
            FireRate = 0.5f;
            MaxAmmo = 7;
        }
    }

    public class Shotgun : ConfiguratedCustomWeapon<ShotgunConfiguration>
    {
        public override ShotgunConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.Shotgun;
        public override string Name => MoreWeaponsMod.Instance.Translations.Shotgun;

        private bool _skip;
        private static bool _mute;

        public override void Shoot(PlayerShootEvent ev)
        {
            if (_skip)
                return;

            base.Shoot(ev);
            if (ev.Cancelled)
                return;
            ev.Cancelled = true;

            var shooter = ev.Player;
            var weaponManager = shooter.weaponManager;
            var transform = weaponManager.camera.transform;

            var itemIndex = shooter.inventory.GetItemIndex();
            shooter.inventory.items.ModifyDuration(itemIndex, shooter.inventory.items[itemIndex].durability + Configuration.Shells);

            var oldUsagesAllowed = weaponManager._iawRateLimit._usagesAllowed;
            AccessTools.Field(typeof(RateLimit), nameof(RateLimit._usagesAllowed)).SetValue(weaponManager._iawRateLimit, -1);
            _skip = weaponManager.netIdentity.isLocalPlayer = true;

            for (var i = 0; i < Configuration.Shells; i++)
            {
                var forward = transform.forward;
                var offset = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), forward) * (transform.up * Random.Range(0.0f, (float) Configuration.Accuracy));

                _mute = i >= Configuration.SoundShells;
                weaponManager.CallCmdShoot(null, HitBoxType.NULL, forward + offset, transform.position + forward, Vector3.zero);
                _mute = false;
            }

            _skip = weaponManager.netIdentity.isLocalPlayer = false;
            AccessTools.Field(typeof(RateLimit), nameof(RateLimit._usagesAllowed)).SetValue(weaponManager._iawRateLimit, oldUsagesAllowed);
            shooter.inventory.items.ModifyDuration(itemIndex, 0);
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.RpcConfirmShot))]
        public static class NoisePatch
        {
            public static bool Prefix()
            {
                return !_mute;
            }
        }

        public override void Shot(PlayerShotByPlayerEvent ev)
        {
            if (ev.Shooter == ev.Player)
            {
                Logger.Debug("Shotgun shot his owner :c");
                ev.Cancelled = true;
                return;
            }

            base.Shot(ev);
            ev.Damage *= Configuration.DamageMultiplier;
        }
    }
}
