// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Deterministic event-content predicates shared by reflection and vector adapters.</summary>
internal static class ContentRules
{
    static readonly string[] _forbiddenNames = ["token", "code", "password", "passwd", "pwd", "secret", "key", "credential", "assertion", "cookie", "nonce", "private", "certificate", "recovery", "personalaccesstoken", "patvalue"];
    static readonly string[] _personalNames = ["email", "emailaddress", "name", "displayname", "fullname", "givenname", "familyname", "login", "loginname", "idpsub", "upstreamsubject", "upstreamsubjectid"];
    static readonly string[] _personalConcepts = ["email", "personalname", "upstreamsubject"];
    static readonly string[] _upstreamNames = ["idpsub", "upstreamsubject", "upstreamsubjectid", "login", "loginname"];

    /// <summary>Checks a shape without reading event values.</summary>
    /// <param name="shape">The declared schema.</param>
    /// <param name="registry">Reviewed leaf exceptions.</param>
    /// <returns>Distinct, ordered content violations.</returns>
    internal static IReadOnlyList<Violation> Check(EventShape shape, IReadOnlyDictionary<string, SafeConceptEntry> registry) =>
        shape.Members.SelectMany(member => CheckMember(shape, member, member.ClrName, registry)).Distinct()
            .OrderBy(violation => violation.Path, StringComparer.Ordinal).ThenBy(violation => violation.ReasonId, StringComparer.Ordinal).ToArray();

    /// <summary>Applies the catalogue's ASCII name normalization.</summary>
    /// <param name="name">The member name or alias.</param>
    /// <returns>The normalized ASCII name.</returns>
    internal static string Normalize(string name) => new(name.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    static IEnumerable<Violation> CheckMember(EventShape shape, MemberShape member, string path, IReadOnlyDictionary<string, SafeConceptEntry> registry)
    {
        var conceptName = member.Concept?.Split('.', '+')[^1] ?? string.Empty;
        var names = new[] { Normalize(member.ClrName), Normalize(member.WireName), Normalize(conceptName) };
        var reason = StructuralReason(member, names, registry);
        if (reason is not null) yield return new(path, reason);
        var lookup = member.Concept is not null && registry.TryGetValue(member.Concept, out var entry) && entry.Kind == "upstream-lookup-digest";
        var personal = lookup || names.Any(_personalNames.Contains) || _personalConcepts.Contains(Normalize(conceptName));
        if (personal && !member.Pii) yield return new(path, "missing_pii");
        if ((personal || member.Pii) && !shape.ComplianceSubject) yield return new(path, "unresolved_compliance_subject");
        if (shape.Visibility == "Public" && names.Any(_upstreamNames.Contains)) yield return new(path, "public_upstream_identifier");
        foreach (var child in member.Members)
        {
            foreach (var violation in CheckMember(shape, child, $"{path}.{child.ClrName}", registry))
            {
                yield return violation;
            }
        }
    }

    static string? StructuralReason(MemberShape member, string[] names, IReadOnlyDictionary<string, SafeConceptEntry> registry)
    {
        if (member.ForbiddenType) return "forbidden_payload_type";
        if (member.OpenShape) return "open_payload_shape";
        SafeConceptEntry? entry = null;
        var registered = member.Concept is not null && registry.TryGetValue(member.Concept, out entry);
        if (names.Any(name => name.Contains("passwordhash", StringComparison.Ordinal) || name.Contains("verifier", StringComparison.Ordinal)) || entry?.Purpose == "password-verification")
        {
            return "password_verifier_forbidden";
        }
        var forbidden = names.Any(name => name == "pat" || name == "state" || _forbiddenNames.Any(fragment => name.Contains(fragment, StringComparison.Ordinal)));
        var digest = names.Any(name => name.EndsWith("hash", StringComparison.Ordinal) || name.EndsWith("thumbprint", StringComparison.Ordinal) || name.EndsWith("fingerprint", StringComparison.Ordinal));
        var exception = digest || registered || (forbidden && names.Any(name => name.EndsWith("id", StringComparison.Ordinal)));
        if (!exception) return forbidden ? "forbidden_member_name" : null;
        if (!registered || entry is null) return "unapproved_safe_concept";
        if (entry.Redeemable) return "redeemable_reference";
        if (entry.Kind == "secret-hash" && !entry.Keyed) return "unkeyed_secret_hash";
        return ValidEntry(member, entry) ? null : "unapproved_safe_concept";
    }

    static bool ValidEntry(MemberShape member, SafeConceptEntry entry)
    {
        if (member.Members.Count != 0 || member.UnderlyingType != entry.UnderlyingType || string.IsNullOrWhiteSpace(entry.ProducerSpec) || string.IsNullOrWhiteSpace(entry.Justification))
        {
            return false;
        }
        return entry.Kind switch
        {
            "reference-id" => entry.Generated && (entry.UnderlyingType == "Guid" || entry.UnderlyingType == "string"),
            "public-key-thumbprint" => entry.UnderlyingType == "string" && entry.Algorithm == "SHA-256" && !string.IsNullOrWhiteSpace(entry.Canonicalization),
            "secret-hash" or "upstream-lookup-digest" => entry.UnderlyingType == "string" && entry.Algorithm == "HMAC-SHA-256" && entry.Keyed && HasDigestContext(entry),
            _ => false
        };
    }

    static bool HasDigestContext(SafeConceptEntry entry) => !string.IsNullOrWhiteSpace(entry.Purpose) && !string.IsNullOrWhiteSpace(entry.HashKeyRevision);
}
