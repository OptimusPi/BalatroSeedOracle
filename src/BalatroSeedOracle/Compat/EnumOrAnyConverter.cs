using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Motely.Filters.Jaml.Converters;

/// <summary>
/// AOT-safe YAML scalar converter for <see cref="EnumOrAny{T}"/>. Reads the literal string
/// <c>any</c> (case-insensitive) as the wildcard sentinel, and any other scalar as a strict
/// case-insensitive enum parse via <see cref="Enum.Parse{TEnum}(string, bool)"/>.
/// Register one instance per closed enum type on the deserializer/serializer.
/// </summary>
public sealed class EnumOrAnyConverter<T> : IYamlTypeConverter
    where T : struct, Enum
{
    public bool Accepts(Type type) => type == typeof(EnumOrAny<T>) || type == typeof(EnumOrAny<T>?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;
        if (string.Equals(value, "any", StringComparison.OrdinalIgnoreCase))
            return EnumOrAny<T>.Any;
        if (Enum.TryParse<T>(value, ignoreCase: true, out var parsed))
            return EnumOrAny<T>.Of(parsed);

        // Enum.Parse throws a bare ArgumentException that YamlDotNet flattens into the
        // useless "Exception during deserialization"; throw a YamlException carrying the
        // scalar's mark so the loader can surface the offending value and the naming rule.
        var names = Enum.GetNames<T>();
        var hint =
            names.Length <= 25
                ? $" Expected 'Any' or one of: {string.Join(", ", names)}."
                : " Expected 'Any' or a PascalCase identifier with no spaces or punctuation"
                    + " (e.g. WeeJoker, not 'Wee Joker').";
        throw new YamlException(
            scalar.Start,
            scalar.End,
            $"'{value}' is not a valid {typeof(T).Name}.{hint}"
        );
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar(""));
            return;
        }
        var v = (EnumOrAny<T>)value;
        emitter.Emit(new Scalar(v.IsAny ? "any" : v.Value.ToString()));
    }
}
