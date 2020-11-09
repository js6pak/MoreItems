using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Grenades;
using HarmonyLib;
using Mirror;
using SixModLoader;
using SixModLoader.Events;
using UnityEngine;
using Logger = SixModLoader.Logger;

namespace MoreItems.Grenades
{
    public class GrenadeExplodeEvent : Event, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public ReferenceHub Thrower { get; }
        public Grenade Grenade { get; }

        public GrenadeExplodeEvent(ReferenceHub thrower, Grenade grenade)
        {
            Thrower = thrower;
            Grenade = grenade;
        }
    }

    public class CustomGrenadeManager
    {
        public MoreItemsMod Mod { get; }

        public CustomGrenadeManager(MoreItemsMod mod)
        {
            Mod = mod;
            SixModLoader.SixModLoader.Instance.EventManager.Register(this);
        }

        public Dictionary<Grenade, Tuple<CustomGrenade, GrenadeExplodeEvent>> Exploding { get; } = new Dictionary<Grenade, Tuple<CustomGrenade, GrenadeExplodeEvent>>();

        [HarmonyPatch]
        public static class ServerThrowGrenadePatch
        {
            public static Grenade Invoke(Grenade grenade, GrenadeManager grenadeManager)
            {
                Logger.Warn(nameof(ServerThrowGrenadePatch));
                try
                {
                    if (MoreItemsMod.Instance.ItemManager.Held.GetValueSafe(grenadeManager.hub) is CustomGrenade customGrenade)
                    {
                        Logger.Warn("throw " + customGrenade);
                        MoreItemsMod.Instance.GrenadeManager.Exploding[grenade] = new Tuple<CustomGrenade, GrenadeExplodeEvent>(customGrenade, new GrenadeExplodeEvent(grenadeManager.hub, grenade));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                return grenade;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(ServerThrowGrenadePatch), nameof(Invoke));
            private static readonly MethodInfo m_Spawn = AccessTools.Method(typeof(NetworkServer), nameof(NetworkServer.Spawn), new[] { typeof(GameObject) });

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(AccessTools.Inner(typeof(GrenadeManager), "<_ServerThrowGrenade>d__9"), "MoveNext");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions.FindIndex(x => x.Calls(m_Spawn)) - 1;

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_1), // load this
                    new CodeInstruction(OpCodes.Call, m_Invoke) // call event
                });

                return codeInstructions;
            }
        }

        [HarmonyPatch]
        public static class ServersideExplosionPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                var types = typeof(Grenade).Assembly.GetLoadableTypes();
                return types.Where(x => x.IsSubclassOf(typeof(Grenade))).Select(x => AccessTools.Method(x, nameof(Grenade.ServersideExplosion), new Type[0]));
            }

            public static bool Prefix(Grenade __instance, MethodBase __originalMethod)
            {
                try
                {
                    Logger.Warn(nameof(ServersideExplosionPatch));
                    if (MoreItemsMod.Instance.GrenadeManager.Exploding.TryGetValue(__instance, out var tuple))
                    {
                        Logger.Warn("explode " + __instance.name);
                        tuple.Item1.Explode(tuple.Item2);
                        MoreItemsMod.Instance.GrenadeManager.Exploding.Remove(__instance);

                        return !tuple.Item2.Cancelled;
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