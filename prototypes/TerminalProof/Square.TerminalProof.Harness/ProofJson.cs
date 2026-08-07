using System.Text.Json;
using System.Text.Json.Serialization;

namespace Square.TerminalProof.Harness;

internal static class ProofJson
{
    internal static JsonSerializerOptions Create(bool indented = false) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = indented
    };
}
