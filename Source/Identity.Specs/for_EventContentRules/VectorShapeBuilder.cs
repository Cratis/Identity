// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.for_EventContentRules;

internal static class VectorShapeBuilder
{
    internal static EventShape Build(JsonElement given, JsonElement context)
    {
        var subject = given.TryGetProperty("complianceSubject", out var overrideSubject) ? overrideSubject : context.GetProperty("complianceSubject");
        var value = given.GetProperty("event");
        given.TryGetProperty("concepts", out var concepts);
        given.TryGetProperty("registry", out var registry);
        return new(
            Text(value, "name"),
            context.GetProperty("typeId").GetString(),
            context.GetProperty("generation").GetUInt32(),
            Text(given, "eventMarker") ?? Text(context, "eventMarker"),
            Text(given, "visibility") ?? Text(context, "visibility"),
            subject.ValueKind != JsonValueKind.Null,
            value.GetProperty("properties").EnumerateArray().Select(property => Member(property, concepts, registry)).ToArray());
    }

    internal static IReadOnlyDictionary<string, SafeConceptEntry> Registry(JsonElement given)
    {
        var result = new Dictionary<string, SafeConceptEntry>();
        if (!given.TryGetProperty("registry", out var entries)) return result;
        foreach (var entry in entries.EnumerateObject())
        {
            var value = entry.Value;
            result.Add(entry.Name, new(
                Primitive(Text(value, "underlyingType")),
                Text(value, "kind"),
                Text(value, "producerSpec") ?? string.Empty,
                Text(value, "justification") ?? string.Empty,
                Text(value, "algorithm"),
                Flag(value, "keyed"),
                Flag(value, "redeemable"),
                Flag(value, "generated"),
                Text(value, "canonicalization"),
                Text(value, "purpose"),
                Text(value, "hashKeyRevision"),
                Text(value, "inputEntropy")));
        }
        return result;
    }

    static MemberShape Member(JsonElement value, JsonElement concepts, JsonElement registry)
    {
        var type = Text(value, "type");
        var primitive = type.StartsWith("System.", StringComparison.Ordinal);
        var definition = Definition(concepts, type);
        var entry = Definition(registry, type);
        var pii = Flag(value, "pii") || (definition.ValueKind == JsonValueKind.Object && definition.TryGetProperty("attributes", out var attributes) &&
            attributes.EnumerateArray().Any(attribute => attribute.GetString() == "Cratis.Chronicle.Compliance.GDPR.PIIAttribute"));
        return new(
            Text(value, "name"),
            Text(value, "wireName") ?? Text(value, "name"),
            type,
            pii,
            type == "System.Byte[]" || type == "System.Security.Cryptography.RSAParameters" || type == "System.Security.Cryptography.ECParameters" || type == "System.IO.Stream",
            type == "System.Object" || type == "System.Text.Json.JsonElement",
            false,
            primitive ? null : type,
            primitive ? null : Primitive(Text(entry, "underlyingType") ?? Text(definition, "underlyingType") ?? "System.String"),
            value.TryGetProperty("properties", out var properties) ? properties.EnumerateArray().Select(property => Member(property, concepts, registry)).ToArray() : []);
    }

    static JsonElement Definition(JsonElement values, string name) => values.ValueKind == JsonValueKind.Object && values.TryGetProperty(name, out var value) ? value : default;
    static string Primitive(string type) => type == "System.String" ? "string" : type == "System.Guid" ? "Guid" : type;
    static string? Text(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property) ? property.GetString() : null;
    static bool Flag(JsonElement value, string name) => value.TryGetProperty(name, out var property) && property.GetBoolean();
}
