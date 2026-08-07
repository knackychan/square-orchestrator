using System.Text.Json;

namespace Square.SharedUiProof.WebView2;

internal sealed record ValidatedBridgeMessage(string Type, JsonElement Root);

internal static class BridgeValidator
{
    private const string Protocol = "square.shared-ui-proof/1";
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Fields =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["proof.ready"] = Set("version", "type", "host"),
            ["proof.result"] = Set("version", "type", "runId", "fixtureSha256", "result"),
            ["proof.error"] = Set("version", "type", "code", "message"),
            ["proof.layoutChanged"] = Set("version", "type", "preset", "selectedTerminalId"),
            ["proof.controllerRequested"] = Set("version", "type", "terminalId"),
            ["terminal.input"] = Set("version", "type", "terminalId", "leaseId", "data"),
            ["terminal.resize"] = Set("version", "type", "terminalId", "leaseId", "columns", "rows")
        };

    public static ValidatedBridgeMessage Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64
        });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Bridge message must be an object.");
        }
        var version = RequiredString(root, "version");
        if (!string.Equals(version, Protocol, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Incompatible bridge version '{version}'.");
        }
        var type = RequiredString(root, "type");
        if (!Fields.TryGetValue(type, out var allowed))
        {
            throw new InvalidDataException($"Unknown UI-to-host message type '{type}'.");
        }
        var observed = root.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var unknown = observed.Except(allowed, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var missing = allowed.Except(observed, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0 || missing.Length > 0)
        {
            throw new InvalidDataException($"Invalid fields for '{type}'. Unknown=[{string.Join(",", unknown)}], missing=[{string.Join(",", missing)}].");
        }
        return new ValidatedBridgeMessage(type, root.Clone());
    }

    private static string RequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"'{name}' must be a string.");
        }
        return property.GetString() ?? throw new InvalidDataException($"'{name}' is null.");
    }

    private static IReadOnlySet<string> Set(params string[] names) => names.ToHashSet(StringComparer.Ordinal);
}
