using System.Text.Json;
using System.Text.Json.Serialization;

namespace Square.Contracts.Serialization;

public static class SquareJson
{
    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = false
        };
        options.Converters.Add(new StrongIdJsonConverterFactory());
        options.Converters.Add(new ContentHashJsonConverter());
        options.Converters.Add(new UtcInstantJsonConverter());
        options.Converters.Add(new SchemaVersionJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}
