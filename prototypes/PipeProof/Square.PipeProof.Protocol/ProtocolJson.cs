using System.Text.Json;
using System.Text.Json.Serialization;

namespace Square.PipeProof.Protocol;

public static class ProtocolJson
{
    public static JsonSerializerOptions Compact { get; } = CreateOptions(writeIndented: false);
    public static JsonSerializerOptions Indented { get; } = CreateOptions(writeIndented: true);
    public static JsonSerializerOptions Options => Compact;

    public static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Compact);

    public static string SerializeText<T>(T value) => JsonSerializer.Serialize(value, Compact);

    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<T>(utf8Json, Compact)
        ?? throw new InvalidDataException($"JSON did not contain a {typeof(T).Name} value.");

    public static T DeserializeText<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Compact)
        ?? throw new InvalidDataException($"JSON did not contain a {typeof(T).Name} value.");

    public static JsonElement ToElement<T>(T value) => JsonSerializer.SerializeToElement(value, Compact);

    public static T FromElement<T>(JsonElement value) =>
        value.Deserialize<T>(Compact)
        ?? throw new InvalidDataException($"JSON did not contain a {typeof(T).Name} value.");

    private static JsonSerializerOptions CreateOptions(bool writeIndented) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        WriteIndented = writeIndented
    };
}
