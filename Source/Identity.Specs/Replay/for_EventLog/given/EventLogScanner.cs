// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

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

    static readonly string[] _secretProperties = ["token", "secret", "password", "authorization", "code", "assertion", "privatekey", "credential", "verifier", "nonce", "cookie"];

    internal static IReadOnlyList<(string EventType, string Rule)> Findings(IEnumerable<string> serializedEvents, IEnumerable<string> knownSecrets)
    {
        var findings = new List<(string EventType, string Rule)>();
        var secrets = knownSecrets.Where(secret => !string.IsNullOrEmpty(secret)).ToArray();
        foreach (var serialized in serializedEvents)
        {
            using var document = JsonDocument.Parse(serialized);
            var root = document.RootElement;
            var type = root.TryGetProperty("eventType", out var name) ? name.GetString() ?? "unknown" : "unknown";
            Scan(root, new ScanContext(type, secrets, findings, "$"));
        }

        return findings;
    }

    static void Scan(JsonElement element, ScanContext context)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            ScanProperties(element, context);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray()) Scan(child, context with { Path = $"{context.Path}[]" });
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            ScanValue(element.GetString(), context);
        }
    }

    static void ScanProperties(JsonElement element, ScanContext context)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (IsSecretProperty(property) && !ReviewedLeaf(context, property)) context.Findings.Add((context.EventType, "secret-property"));
            Scan(property.Value, context with { Path = $"{context.Path}.{property.Name}" });
        }
    }

    static bool ReviewedLeaf(ScanContext context, JsonProperty property)
    {
        if (context.EventType != typeof(ClientSecretRotated).FullName || context.Path != "$.content") return false;
        var value = UnwrapConcept(property.Value);
        if (value.ValueKind != JsonValueKind.String) return false;
        return property.Name switch
        {
            "ClientSecretHash" => SecretDigest.IsWellFormed(value.GetString()),
            "ClientSecretId" => Guid.TryParseExact(value.GetString(), "D", out var id) && id != Guid.Empty,
            _ => false
        };
    }

    static JsonElement UnwrapConcept(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 1) return value;
        return value.TryGetProperty("Value", out var primitive) ? primitive : value;
    }

    static bool IsSecretProperty(JsonProperty property)
    {
        var name = property.Name.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return _secretProperties.Any(secret => name.Contains(secret, StringComparison.Ordinal)) &&
            property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) &&
            (property.Value.ValueKind != JsonValueKind.String || !string.IsNullOrEmpty(property.Value.GetString()));
    }

    static void ScanValue(string value, ScanContext context)
    {
        foreach (var (rule, pattern) in _rules)
        {
            if (pattern.IsMatch(value)) context.Findings.Add((context.EventType, rule));
        }

        if (context.Secrets.Any(secret => value.Contains(secret, StringComparison.OrdinalIgnoreCase))) context.Findings.Add((context.EventType, "known-secret"));
    }

    sealed record ScanContext(string EventType, string[] Secrets, List<(string EventType, string Rule)> Findings, string Path);

    static Regex Pattern(string expression) => new(expression, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
