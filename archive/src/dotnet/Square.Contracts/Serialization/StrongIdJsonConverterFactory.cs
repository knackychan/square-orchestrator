using System.Text.Json;
using System.Text.Json.Serialization;
using Square.Domain.Primitives;

namespace Square.Contracts.Serialization;

public sealed class StrongIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsValueType && typeToConvert.GetInterfaces().Any(
        contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IStrongId<>));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(StrongIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)(Activator.CreateInstance(converterType, nonPublic: true)
            ?? throw new InvalidOperationException($"Cannot create a strong-ID converter for {typeToConvert}."));
    }

    private sealed class StrongIdJsonConverter<TStrongId> : JsonConverter<TStrongId>
        where TStrongId : struct, IStrongId<TStrongId>
    {
        public override TStrongId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (!TStrongId.TryParse(value, out TStrongId parsed)) throw new JsonException($"Invalid {typeof(TStrongId).Name} value '{value}'.");
            return parsed;
        }
        public override void Write(Utf8JsonWriter writer, TStrongId value, JsonSerializerOptions options)
        {
            if (string.IsNullOrEmpty(value.Value)) throw new JsonException($"Default {typeof(TStrongId).Name} values cannot be serialized.");
            writer.WriteStringValue(value.Value);
        }
    }
}
