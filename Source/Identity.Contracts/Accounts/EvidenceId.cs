// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>An opaque verified-evidence reference; never proof material.</summary>
/// <param name="Value">The reference.</param>
public record EvidenceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts a reference.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator EvidenceId(Guid value) => new(value);
}
