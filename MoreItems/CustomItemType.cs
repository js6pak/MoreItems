using System;
using System.ComponentModel;
using System.Linq;
using SixModLoader.Api.Configuration.Converters;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MoreItems
{
    public class BaseItemTypeConverter : EventYamlTypeConverter
    {
        public override bool Accepts(Type type)
        {
            return typeof(BaseItemType).IsAssignableFrom(type);
        }

        public override object ReadYaml(IParser parser, Type type)
        {
            var @event = parser.Current;
            if (@event == null)
                throw new YamlException("Parser event can't be null!");

            var id = parser.Consume<Scalar>().Value;

            if (Enum.TryParse<ItemType>(id, true, out var vanilla))
            {
                return new VanillaItemType(vanilla);
            }

            if (CustomItem.Items.TryGetValue(id, out var customItem))
            {
                return new CustomItemType(customItem);
            }

            throw new YamlException(@event.Start, @event.End, $"Item {id} not found!");
        }

        public override void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var eventEmitter = EventEmitter.Invoke();
            eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor(value.ToString(), typeof(string), value.GetType())), emitter);
        }
    }

    [TypeConverter(typeof(BaseItemTypeConverter))]
    public abstract class BaseItemType
    {
    }

    public class CustomItemType : BaseItemType
    {
        public string Id { get; }
        public Type Type { get; }

        public override string ToString()
        {
            return Id;
        }

        public CustomItemType(Type type)
        {
            Type = type;
            Id = CustomItem.Items.Single(x => x.Value == type).Key;
        }
    }

    public class CustomItemType<T> : CustomItemType where T : CustomItem
    {
        public CustomItemType() : base(typeof(T))
        {
        }
    }

    public class VanillaItemType : BaseItemType
    {
        public ItemType Type { get; }

        public VanillaItemType(ItemType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(ItemType), Type)!;
        }

        public static implicit operator ItemType(VanillaItemType x) => x.Type;
        public static explicit operator VanillaItemType(ItemType x) => new VanillaItemType(x);
    }
}