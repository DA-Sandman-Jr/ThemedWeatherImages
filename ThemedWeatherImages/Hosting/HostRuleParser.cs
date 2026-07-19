using System.Globalization;
using System.Net;

namespace ThemedWeatherImages.Hosting;

public static class HostRuleParser
{
    public static bool TryParse(string value, out ParsedHostEntry entry, out string? error)
    {
        string raw = value.Trim();
        if (raw.Length == 0)
        {
            error = "Host entry is empty.";
            entry = default;
            return false;
        }

        bool allowSubdomains = false;
        if (raw.StartsWith("*.", StringComparison.Ordinal))
        {
            allowSubdomains = true;
            raw = raw[2..];
        }

        if (!TrySplitHostAndPort(raw, out string? host, out int? port, out error))
        {
            entry = default;
            return false;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "Host component is empty.";
            entry = default;
            return false;
        }

        if (IPAddress.TryParse(host, out _))
        {
            error = "IP address hosts are not supported.";
            entry = default;
            return false;
        }

        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            error = "Host entry is invalid.";
            entry = default;
            return false;
        }

        if (allowSubdomains && port.HasValue)
        {
            error = "Wildcard host entries may not specify a port.";
            entry = default;
            return false;
        }

        entry = new ParsedHostEntry(host.ToLowerInvariant(), port, allowSubdomains);
        error = null;
        return true;
    }

    private static bool TrySplitHostAndPort(string value, out string host, out int? port, out string? error)
    {
        host = value;
        port = null;
        error = null;

        int colonIndex = value.LastIndexOf(':');
        if (colonIndex > -1 && colonIndex < value.Length - 1 && !value.Contains(']', StringComparison.Ordinal))
        {
            host = value[..colonIndex];
            string portPart = value[(colonIndex + 1)..];
            if (!int.TryParse(portPart, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
            {
                error = "Port value is invalid.";
                return false;
            }

            port = parsedPort;
        }

        return true;
    }
}

public readonly struct ParsedHostEntry : IEquatable<ParsedHostEntry>
{
    public string Host { get; }
    public int? Port { get; }
    public bool AllowSubdomains { get; }

    public ParsedHostEntry(string host, int? port, bool allowSubdomains)
    {
        Host = host;
        Port = port;
        AllowSubdomains = allowSubdomains;
    }

    public bool MatchesHost(string candidateHost, int? candidatePort)
    {
        string lower = candidateHost.ToLowerInvariant();
        if (AllowSubdomains)
        {
            return lower.EndsWith("." + Host, StringComparison.Ordinal);
        }

        if (!lower.Equals(Host, StringComparison.Ordinal))
        {
            return false;
        }

        return !Port.HasValue || candidatePort == Port;
    }

    public bool Equals(ParsedHostEntry other) =>
        AllowSubdomains == other.AllowSubdomains &&
        string.Equals(Host, other.Host, StringComparison.Ordinal) &&
        Port == other.Port;

    public override bool Equals(object? obj) => obj is ParsedHostEntry other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Host, Port, AllowSubdomains);

    public static bool operator ==(ParsedHostEntry left, ParsedHostEntry right) => left.Equals(right);

    public static bool operator !=(ParsedHostEntry left, ParsedHostEntry right) => !left.Equals(right);
}
