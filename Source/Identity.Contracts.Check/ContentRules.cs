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
        foreach (var violation in PrivacyViolations(shape, member, path, registry)) yield return violation;
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
        var entry = RegisteredEntry(member, registry);
        if (IsPasswordVerifier(names) || entry?.Purpose == "password-verification")
        {
            return "password_verifier_forbidden";
        }
        var forbidden = names.Any(IsForbiddenName);
        if (!RequiresException(names, entry, forbidden)) return forbidden ? "forbidden_member_name" : null;
        return entry is not null ? ExceptionReason(member, entry) : "unapproved_safe_concept";
    }

    static SafeConceptEntry? RegisteredEntry(MemberShape member, IReadOnlyDictionary<string, SafeConceptEntry> registry) =>
        member.Concept is not null && registry.TryGetValue(member.Concept, out var entry) ? entry : null;

    static bool RequiresException(string[] names, SafeConceptEntry? entry, bool forbidden) =>
        entry is not null || names.Any(IsDigestName) || (forbidden && names.Any(name => name.EndsWith("id", StringComparison.Ordinal)));

    static bool IsDigestName(string name) => name.EndsWith("hash", StringComparison.Ordinal) ||
        name.EndsWith("thumbprint", StringComparison.Ordinal) || name.EndsWith("fingerprint", StringComparison.Ordinal);

    static IEnumerable<Violation> PrivacyViolations(EventShape shape, MemberShape member, string path, IReadOnlyDictionary<string, SafeConceptEntry> registry)
    {
        var names = new[] { Normalize(member.ClrName), Normalize(member.WireName), Normalize(member.Concept?.Split('.', '+')[^1] ?? string.Empty) };
        var registered = RegisteredEntry(member, registry);
        var personal = !InternalBusinessLabel(shape, member, registered) && PersonalMember(names, registered);
        if (personal && !member.Pii) yield return new(path, "missing_pii");
        if ((personal || member.Pii) && !shape.ComplianceSubject) yield return new(path, "unresolved_compliance_subject");
        if (shape.Visibility == "Public" && names.Any(_upstreamNames.Contains)) yield return new(path, "public_upstream_identifier");
    }

    static bool InternalBusinessLabel(EventShape shape, MemberShape member, SafeConceptEntry? entry) =>
        shape.Visibility == "Internal" && entry?.Kind == "business-label" && ValidEntry(member, entry);

    static bool PersonalMember(string[] names, SafeConceptEntry? entry) => entry?.Kind == "upstream-lookup-digest" ||
        names.Any(_personalNames.Contains) || _personalConcepts.Contains(names[2]);

    static bool IsForbiddenName(string name) => name == "pat" || name == "state" || _forbiddenNames.Any(fragment => name.Contains(fragment, StringComparison.Ordinal));

    static bool IsPasswordVerifier(string[] names) => names.Any(name => name.Contains("passwordhash", StringComparison.Ordinal) || name.Contains("verifier", StringComparison.Ordinal));

    static string? ExceptionReason(MemberShape member, SafeConceptEntry entry)
    {
        if (entry.Redeemable) return "redeemable_reference";
        if (entry.Kind == "secret-hash" && !entry.Keyed) return "unkeyed_secret_hash";
        return ValidEntry(member, entry) ? null : "unapproved_safe_concept";
    }

    static bool ValidEntry(MemberShape member, SafeConceptEntry entry)
    {
        if (!DocumentedLeaf(member, entry))
        {
            return false;
        }
        return entry.Kind switch
        {
            "business-label" => ValidBusinessLabel(member, entry),
            "reference-id" => entry.Generated && (entry.UnderlyingType == "Guid" || entry.UnderlyingType == "string"),
            "public-key-thumbprint" => ValidThumbprint(entry),
            "secret-hash" or "upstream-lookup-digest" => ValidDigest(entry),
            _ => false
        };
    }

    static bool DocumentedLeaf(MemberShape member, SafeConceptEntry entry) => member.Members.Count == 0 &&
        member.UnderlyingType == entry.UnderlyingType && !string.IsNullOrWhiteSpace(entry.ProducerSpec) && !string.IsNullOrWhiteSpace(entry.Justification);

    static bool ValidBusinessLabel(MemberShape member, SafeConceptEntry entry) =>
        member.Concept == "Cratis.Identity.Contracts.Clients.ClientDisplayName" && Normalize(member.ClrName) == "displayname" &&
        Normalize(member.WireName) == "displayname" && entry.UnderlyingType == "string";

    static bool ValidThumbprint(SafeConceptEntry entry) => entry.UnderlyingType == "string" && entry.Algorithm == "SHA-256" &&
        !string.IsNullOrWhiteSpace(entry.Canonicalization);

    static bool ValidDigest(SafeConceptEntry entry) => entry.UnderlyingType == "string" && entry.Algorithm == "HMAC-SHA-256" &&
        entry.Keyed && HasDigestContext(entry);

    static bool HasDigestContext(SafeConceptEntry entry) => !string.IsNullOrWhiteSpace(entry.Purpose) && !string.IsNullOrWhiteSpace(entry.HashKeyRevision);
}
