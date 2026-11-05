// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>Profile email, never identity proof.</summary>
/// <param name="Value">The email.</param>
[PII, ComplianceDetails("Account profile email, erased with the account subject.")]
public record Email(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a profile email.</summary>
    /// <param name="value">The email.</param>
    public static implicit operator Email(string value) => new(value);
}
