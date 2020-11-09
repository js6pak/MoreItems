using System;
using System.Collections.Generic;
using HarmonyLib;
using SixModLoader.Api.Configuration.Converters;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MoreWeapons
{
    public class SyncItemInfoConverter : EventYamlTypeConverter
    {
        public override bool Accepts(Type type)
        {
            return typeof(Inventory.SyncItemInfo).IsAssignableFrom(type);
        }

        public override object ReadYaml(IParser parser, Type type)
        {
            var @event = parser.Current;
            if (@event == null)
                throw new YamlException("Parser event can't be null!");

            parser.Consume<MappingStart>();

            parser.Consume<Scalar>();
            var id = parser.Consume<Scalar>().Value;

            parser.Consume<Scalar>();
            parser.Consume<MappingStart>();

            var mods = new Dictionary<string, int>();

            while (parser.TryConsume<Scalar>(out var mod))
            {
                mods[mod.Value] = int.Parse(parser.Consume<Scalar>().Value);
            }

            parser.Consume<MappingEnd>();

            parser.Consume<MappingEnd>();

            return new Inventory.SyncItemInfo
            {
                id = (ItemType) Enum.Parse(typeof(ItemType), id, true),
                modBarrel = mods.GetValueSafe("barrel"),
                modSight = mods.GetValueSafe("sight"),
                modOther = mods.GetValueSafe("other")
            };
        }

        public override void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var eventEmitter = EventEmitter.Invoke();

            var item = (Inventory.SyncItemInfo) value;
            eventEmitter.Emit(new MappingStartEventInfo(new ObjectDescriptor(value, value.GetType(), value.GetType())), emitter);

            eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor("id", typeof(string), typeof(string))), emitter);
            eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor(item.id, typeof(ItemType), typeof(ItemType))), emitter);

            eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor("mods", typeof(string), typeof(string))), emitter);

            var mods = new Dictionary<string, int>();

            if (item.modSight != 0)
            {
                mods["sight"] = item.modSight;
            }

            if (item.modBarrel != 0)
            {
                mods["barrel"] = item.modBarrel;
            }

            if (item.modOther != 0)
            {
                mods["other"] = item.modOther;
            }

            eventEmitter.Emit(new MappingStartEventInfo(new ObjectDescriptor(mods, mods.GetType(), mods.GetType())), emitter);

            foreach (var pair in mods)
            {
                eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor(pair.Key, typeof(string), typeof(string))), emitter);
                eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor(pair.Value, typeof(int), typeof(int))), emitter);
            }

            eventEmitter.Emit(new MappingEndEventInfo(new ObjectDescriptor(mods, mods.GetType(), mods.GetType())), emitter);

            eventEmitter.Emit(new MappingEndEventInfo(new ObjectDescriptor(value, value.GetType(), value.GetType())), emitter);
        }
    }
}