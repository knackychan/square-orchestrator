using System.Text.Json;
using System.Text.Json.Serialization;
using Square.Domain.Primitives;

namespace Square.Contracts.Serialization;

public sealed class ContentHashJsonConverter : JsonConverter<ContentHash>
{
    public override ContentHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ContentHash.Parse(reader.GetString() ?? throw new JsonException("A content hash must be a string."));
    public override void Write(Utf8JsonWriter writer, ContentHash value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
public sealed class UtcInstantJsonConverter : JsonConverter<UtcInstant>
{
    public override UtcInstant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        UtcInstant.Parse(reader.GetString() ?? throw new JsonException("A UTC instant must be a string."));
    public override void Write(Utf8JsonWriter writer, UtcInstant value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
public sealed class SchemaVersionJsonConverter : JsonConverter<SchemaVersion>
{
    public override SchemaVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        SchemaVersion.Parse(reader.GetString() ?? throw new JsonException("A schema version must be a string."));
    public override void Write(Utf8JsonWriter writer, SchemaVersion value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
