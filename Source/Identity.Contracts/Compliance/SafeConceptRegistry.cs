// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

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
                HashKeyRevision: "v<revision>.<base64url>")
        }.AsReadOnly();
}
