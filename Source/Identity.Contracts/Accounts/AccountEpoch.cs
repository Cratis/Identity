// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The account's monotonic authorization epoch.</summary>
/// <param name="Value">The epoch.</param>
public record AccountEpoch(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>Converts an epoch.</summary>
    /// <param name="value">The epoch.</param>
    public static implicit operator AccountEpoch(ulong value) => new(value);
}
