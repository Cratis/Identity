// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>A revision-prefixed keyed pseudonym of an exact upstream pair.</summary>
/// <param name="Value">The v&lt;revision&gt;.&lt;base64url&gt; digest.</param>
[PII, ComplianceDetails("Keyed upstream lookup pseudonym, erased with the account subject.")]
public record UpstreamLookupDigest(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a lookup digest.</summary>
    /// <param name="value">The digest.</param>
    public static implicit operator UpstreamLookupDigest(string value) => new(value);
}
