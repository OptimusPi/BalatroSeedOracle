using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Motely.Filters.Jaml.Converters;

/// <summary>
/// Deserializes <see cref="StandardCardValue"/> as either a scalar card id
/// (<c>TenOfHearts</c>) or a mapping (<c>{ rank, suit, ... }</c>).
/// </summary>
public sealed class StandardCardValueConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(StandardCardValue) || type == typeof(StandardCardValue?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar)
        {
            var scalar = parser.Consume<Scalar>();
            return new StandardCardValue { StringValue = scalar.Value };
        }

        if (parser.Current is MappingStart)
        {
            var config = rootDeserializer(typeof(StandardCardConfig));
            return new StandardCardValue { ObjectValue = (StandardCardConfig)config! };
        }

        throw new YamlException(
            "standardCard must be a scalar card name or a mapping with rank/suit (and optional modifiers)."
        );
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not StandardCardValue card)
        {
            emitter.Emit(new Scalar(""));
            return;
        }

        if (!string.IsNullOrWhiteSpace(card.StringValue))
        {
            emitter.Emit(new Scalar(card.StringValue));
            return;
        }

        if (card.ObjectValue is { } cfg)
            serializer(cfg, typeof(StandardCardConfig));
        else
            emitter.Emit(new Scalar(""));
    }
}
