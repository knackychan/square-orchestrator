using System.Security.Cryptography;

namespace Square.SharedUiProof.WebView2;

internal static class HtmlTemplateRenderer
{
    public static string Render(string template)
    {
        const string origin = "https://square-proof.local";
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
        var csp = string.Join("; ",
            "default-src 'none'",
            $"img-src {origin} data:",
            $"style-src {origin}",
            "style-src-attr 'unsafe-inline'",
            $"font-src {origin}",
            $"script-src {origin} 'nonce-{nonce}'",
            "connect-src 'none'",
            "object-src 'none'",
            "base-uri 'none'",
            "form-action 'none'",
            "frame-ancestors 'none'");
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CSP"] = csp,
            ["NONCE"] = nonce,
            ["XTERM_CSS_URI"] = $"{origin}/vendor/xterm.css",
            ["STYLES_URI"] = $"{origin}/styles.css",
            ["XTERM_MODULE_URI"] = $"{origin}/vendor/xterm.mjs",
            ["FIT_MODULE_URI"] = $"{origin}/vendor/addon-fit.mjs",
            ["APP_MODULE_URI"] = $"{origin}/src/web/main.js"
        };
        var result = template;
        foreach (var pair in values)
        {
            result = result.Replace($"{{{{{pair.Key}}}}}", pair.Value, StringComparison.Ordinal);
        }
        if (System.Text.RegularExpressions.Regex.IsMatch(result, "\\{\\{[A-Z0-9_]+\\}\\}"))
        {
            throw new InvalidDataException("Shared UI HTML template contains an unresolved placeholder.");
        }
        return result;
    }
}
