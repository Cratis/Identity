// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>A non-PII, non-redeemable fact reference, stable across retries of an accepted operation.</summary>
/// <param name="Value">The opaque reference.</param>
public record FactId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts an opaque reference.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator FactId(Guid value) => new(value);

    /// <summary>Allocates an operation reference once, to be carried through its retries.</summary>
    /// <returns>The opaque reference.</returns>
    public static FactId New() => new(Guid.NewGuid());

    /// <summary>Derives a distinct fact reference from an accepted operation and fact discriminator.</summary>
    /// <param name="operation">The stable accepted operation reference.</param>
    /// <param name="fact">The source fact name, not personal content.</param>
    /// <returns>The deterministic fact reference.</returns>
    public static FactId ForOperation(FactId operation, string fact)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"cratis.identity.fact.v1:{operation.Value:D}:{fact}"));
        return new(new Guid(digest.AsSpan(0, 16)));
    }
}
