// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The account's mutable profile name.</summary>
/// <param name="Value">The name.</param>
[PII, ComplianceDetails("Account profile name, erased with the account subject.")]
public record PersonalName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a profile name.</summary>
    /// <param name="value">The name.</param>
    public static implicit operator PersonalName(string value) => new(value);
}
