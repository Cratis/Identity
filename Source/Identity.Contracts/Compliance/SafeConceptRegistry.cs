// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Resources;

namespace Cratis.Identity.Contracts.Compliance;

/// <summary>
/// Reviewed exceptions for event content; entries are producer evidence, not payload fields.
/// </summary>
public static class SafeConceptRegistry
{
    /// <summary>
    /// Gets the reviewed leaves, keyed by fully qualified concept identity.
    /// </summary>
    public static IReadOnlyDictionary<string, SafeConceptEntry> Entries { get; } =
        new Dictionary<string, SafeConceptEntry>
        {
            [typeof(UpstreamLookupDigest).FullName!] = new(
                "string",
                "upstream-lookup-digest",
                "identity-events-v1 §6.2; Accounts/Lookup/UpstreamKeyDigester and LookupRevisionGuard",
                "Exact UTF-8 issuer/subject are length framed; the key is distinct from token and credential keys; revision reuse is refused.",
                Algorithm: "HMAC-SHA-256",
                Keyed: true,
                Redeemable: false,
                Purpose: "cratis.identity.upstream-key-lookup.v1",
                HashKeyRevision: "v<revision>.<base64url>"),
            [typeof(ClientRegistrationId).FullName!] = Reference("Generated UUID client stream identity, independent of the protocol client id."),
            [typeof(ResourceId).FullName!] = Reference("Generated UUID resource stream identity, independent of indicator and audience."),
            [typeof(ClientSecretId).FullName!] = Reference("Generated UUID secret revision reference, never secret material."),
            [typeof(SecretDigest).FullName!] = new(
                "string",
                "secret-hash",
                "identity-events-v1 §9.6; keyed producer deferred to D3 (not an implemented writer attestation)",
                "Only producer-computed HMAC-SHA-256 is admissible; format checks cannot prove keyed provenance.",
                Algorithm: "HMAC-SHA-256",
                Keyed: true,
                Redeemable: false,
                Purpose: "cratis.identity.client-secret.v1",
                HashKeyRevision: "v<revision>.<base64url>"),
            [typeof(ClientDisplayName).FullName!] = new(
                "string",
                "business-label",
                "identity-contracts-v1.1-amendment §3 and decision 4",
                "Internal non-person application display label only.",
                Redeemable: false),
            [typeof(ResourceDisplayName).FullName!] = new(
                "string",
                "business-label",
                "identity-contracts-v1.1-amendment decision 4; RegisterResource label admission",
                "Internal non-person resource display label only; never accepted on a Public event.",
                Redeemable: false)
        }.AsReadOnly();

    static SafeConceptEntry Reference(string justification) => new(
        "Guid",
        "reference-id",
        "Contracts ClientRegistrationId.New, ResourceId.New and ClientSecretId.New; D2 allocation specs",
        justification,
        Redeemable: false,
        Generated: true);
}
