// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>A mutable upstream display identifier; never a matching or authorization key.</summary>
/// <param name="Value">The display identifier.</param>
[PII, ComplianceDetails("Per-key display identifier and preferred_username, erased with the account subject.")]
public record Login(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a display identifier.</summary>
    /// <param name="value">The identifier.</param>
    public static implicit operator Login(string value) => new(value);
}
