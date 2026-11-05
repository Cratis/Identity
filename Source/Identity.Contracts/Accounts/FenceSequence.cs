// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The durable revocation-fence sequence.</summary>
/// <param name="Value">The sequence.</param>
public record FenceSequence(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>Converts a sequence.</summary>
    /// <param name="value">The sequence.</param>
    public static implicit operator FenceSequence(ulong value) => new(value);
}
