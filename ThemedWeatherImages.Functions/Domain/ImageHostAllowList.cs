using System.Net;
using ThemedWeatherImages.Hosting;

namespace ThemedWeatherImages.Functions.Domain;

public sealed class ImageHostAllowList
{
    // Default to the canonical AI Horde CDN domains when no explicit allow-list is configured.
    private static readonly string[] DefaultAllowListEntries =
    [
        "*.aihorde.net",
        "*.stablehorde.net",
        "stablehorde.net",
        "*.r2.cloudflarestorage.com"
    ];

    private readonly IReadOnlyList<ImageHostRule> _rules;

    private ImageHostAllowList(IReadOnlyList<ImageHostRule> rules)
    {
        _rules = rules;
    }

    // Public so consuming Functions hosts (separate assemblies) can construct
    // the allow list from their own configuration during startup.
    public static bool TryCreate(string? rawValue, out ImageHostAllowList allowList, out string? error)
    {
        string[] entries = string.IsNullOrWhiteSpace(rawValue)
            ? DefaultAllowListEntries
            : rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (entries.Length == 0)
        {
            error = "AIHORDE_ALLOWED_IMAGE_HOSTS does not contain any hosts.";
            allowList = null!;
            return false;
        }

        var rules = new List<ImageHostRule>(entries.Length);

        foreach (string entry in entries)
        {
            if (!ImageHostRule.TryParse(entry, out ImageHostRule? rule, out string? parseError))
            {
                error = parseError;
                allowList = null!;
                return false;
            }

            rules.Add(rule);
        }

        allowList = new ImageHostAllowList(rules);
        error = null;
        return true;
    }

    public bool TryGetTrustedUri(string rawUri, out Uri imageUri, out string? rejectionReason)
    {
        imageUri = null!;
        rejectionReason = null;

        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out Uri? candidateUri))
        {
            rejectionReason = "URI is not absolute.";
            return false;
        }

        if (!string.Equals(candidateUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            rejectionReason = "URI must use HTTPS.";
            return false;
        }

        if (IPAddress.TryParse(candidateUri.Host, out _))
        {
            rejectionReason = "IP address hosts are not permitted.";
            return false;
        }

        if (!_rules.Any(rule => rule.Matches(candidateUri)))
        {
            rejectionReason = "Host is not in the allow-list.";
            return false;
        }

        imageUri = candidateUri;
        return true;
    }
}

internal sealed class ImageHostRule
{
    private readonly ParsedHostEntry _entry;

    private ImageHostRule(ParsedHostEntry entry)
    {
        _entry = entry;
    }

    public static bool TryParse(string value, out ImageHostRule rule, out string? error)
    {
        if (!HostRuleParser.TryParse(value, out ParsedHostEntry entry, out error))
        {
            rule = null!;
            return false;
        }

        rule = new ImageHostRule(entry);
        return true;
    }

    public bool Matches(Uri uri) => _entry.MatchesHost(uri.Host, uri.Port);
}
