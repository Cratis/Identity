// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Identity.Replay.for_EventLog.given;

internal static class EventLogScanner
{
    static readonly (string Rule, Regex Pattern)[] _rules =
    [
        ("jwt", Pattern(@"eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.")),
        ("pat", Pattern("[a-z0-9-]+_pat_v1_[0-9a-zA-Z]{16,}_[0-9a-f]{8}")),
        ("github", Pattern("gh[pousr]_[A-Za-z0-9]{20,}|github_pat_")),
        ("authorization", Pattern("(?:Bearer|DPoP) [A-Za-z0-9._~-]{16,}")),
        ("pem", Pattern("-----BEGIN")),
        ("opaque", Pattern("^[A-Za-z0-9_-]{40,}$"))
    ];

    static readonly string[] _secretProperties = ["client_secret", "refresh_token", "access_token", "authorization_code", "password", "secret"];

    internal static IReadOnlyList<(string EventType, string Rule)> Findings(IEnumerable<string> serializedEvents, IEnumerable<string> knownSecrets)
    {
        var findings = new List<(string EventType, string Rule)>();
        var secrets = knownSecrets.Where(secret => !string.IsNullOrEmpty(secret)).ToArray();
        foreach (var serialized in serializedEvents)
        {
            using var document = JsonDocument.Parse(serialized);
            var root = document.RootElement;
            var type = root.TryGetProperty("eventType", out var name) ? name.GetString() ?? "unknown" : "unknown";
            Scan(root, type, secrets, findings);
        }

        return findings;
    }

    static void Scan(JsonElement element, string type, string[] secrets, List<(string EventType, string Rule)> findings)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            ScanProperties(element, type, secrets, findings);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray()) Scan(child, type, secrets, findings);
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            ScanValue(element.GetString(), type, secrets, findings);
        }
    }

    static void ScanProperties(JsonElement element, string type, string[] secrets, List<(string EventType, string Rule)> findings)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (IsSecretProperty(property)) findings.Add((type, "secret-property"));
            Scan(property.Value, type, secrets, findings);
        }
    }

    static bool IsSecretProperty(JsonProperty property) =>
        _secretProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase) &&
        property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(property.Value.GetString());

    static void ScanValue(string value, string type, string[] secrets, List<(string EventType, string Rule)> findings)
    {
        foreach (var (rule, pattern) in _rules)
        {
            if (pattern.IsMatch(value)) findings.Add((type, rule));
        }

        if (secrets.Any(secret => value.Contains(secret, StringComparison.OrdinalIgnoreCase))) findings.Add((type, "known-secret"));
    }

    static Regex Pattern(string expression) => new(expression, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
