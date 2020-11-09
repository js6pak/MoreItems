using System.Linq;
using Grenades;
using MoreItems.Grenades;
using UnityEngine;
using Logger = SixModLoader.Logger;

namespace MoreWeapons.Grenades
{
    public abstract class CustomFragGrenade : CustomGrenade
    {
        private static readonly FragGrenade _fragGrenade;

        static CustomFragGrenade()
        {
            var settings = PlayerManager.localPlayer.GetComponent<GrenadeManager>().availableGrenades.Single(x => x.inventoryID == ItemType.GrenadeFrag);
            _fragGrenade = settings.grenadeInstance.GetComponent<FragGrenade>();
            _fragGrenade.hurtLayerMask = ~_fragGrenade.hurtLayerMask;
        }

        public override void Explode(GrenadeExplodeEvent ev)
        {
            base.Explode(ev);
            Logger.Warn("cancel explode");
            ev.Cancelled = true;

            var position = ev.Grenade.transform.position;
            foreach (var player in ReferenceHub.Hubs.Values)
            {
                if (!ServerConsole.FriendlyFire && player != ev.Thrower && !player.GetComponent<WeaponManager>().GetShootPermission(ev.Thrower.characterClassManager.CurRole.team))
                    continue;

                if (!player.characterClassManager.InWorld)
                    continue;

                if (Vector3.Distance(position, player.transform.position) > 10)
                    continue;

                foreach (var grenadePoint in player.playerStats.grenadePoints)
                {
                    if (!Physics.Linecast(position, grenadePoint.position, _fragGrenade.hurtLayerMask))
                    {
                        Explode(player);
                        break;
                    }
                }
            }
        }

        public abstract void Explode(ReferenceHub player);
    }
}