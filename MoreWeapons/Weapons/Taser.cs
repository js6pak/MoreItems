using System;
using System.Collections.Generic;
using System.ComponentModel;
using CustomPlayerEffects;
using HarmonyLib;
using MEC;
using Mirror;
using SixModLoader.Api.Events.Player.Weapon;
using UnityEngine;
using Logger = SixModLoader.Logger;

namespace MoreWeapons.Weapons
{
    public class TaserConfiguration : WeaponConfiguration
    {
        [Description("Freeze time after shot in seconds")]
        public float FreezeTime { get; set; } = 5;

        [Description("Drop items after tase")]
        public bool DropItems { get; set; } = true;

        public TaserConfiguration()
        {
            BaseItem = new Inventory.SyncItemInfo { id = ItemType.GunUSP };
            FireRate = 2f;
            MaxAmmo = 2;
        }
    }

    public class Taser : ConfiguratedCustomWeapon<TaserConfiguration>
    {
        public override TaserConfiguration Configuration => MoreWeaponsMod.Instance.Configuration.Taser;
        public override string Name => MoreWeaponsMod.Instance.Translations.Taser;

        private static List<ReferenceHub> FrozenPlayers { get; } = new List<ReferenceHub>();

        public override void Shot(PlayerShotByPlayerEvent ev)
        {
            base.Shot(ev);
            if (ev.Cancelled)
                return;

            var target = ev.Player.GetComponent<ReferenceHub>();
            ev.Damage = target.playerStats.maxHP * 0.01f;

            if (Configuration.DropItems)
            {
                target.inventory.ServerDropAll();
            }

            if (target.characterClassManager.CurRole.team == Team.SCP)
                return;

            TargetShake(target.playerMovementSync.connectionToClient, true);

            FrozenPlayers.Add(target);
            target.playerEffectsController.EnableEffect<Ensnared>();

            Timing.CallDelayed(Configuration.FreezeTime, () =>
            {
                FrozenPlayers.Remove(target);
                target.playerEffectsController.DisableEffect<Ensnared>();
            });
        }

        private void TargetShake(NetworkConnection connection, bool achieve)
        {
            var networkBehaviour = AlphaWarheadController.Host;
            var writer = NetworkWriterPool.GetWriter();
            writer.WriteBoolean(achieve);
            var msg = new RpcMessage
            {
                netId = networkBehaviour.netId,
                componentIndex = networkBehaviour.ComponentIndex,
                functionHash = NetworkBehaviour.GetMethodHash(typeof(AlphaWarheadController), nameof(AlphaWarheadController.RpcShake)),
                payload = writer.ToArraySegment()
            };
            connection.Send(msg);
            NetworkWriterPool.Recycle(writer);
        }

        public override void Reload(PlayerWeaponReloadEvent ev)
        {
            ev.Cancelled = true;
        }

        [HarmonyPatch(typeof(PlayerMovementSync), nameof(PlayerMovementSync.ReceivePosition), typeof(Vector3))]
        public static class CallCmdSendPositionPatch
        {
            public static bool Prefix(PlayerMovementSync __instance)
            {
                try
                {
                    if (FrozenPlayers.Contains(ReferenceHub.GetHub(__instance.gameObject)))
                    {
                        __instance.TargetForcePosition(__instance.connectionToClient, __instance.GetRealPosition());
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                return true;
            }
        }
    }
}