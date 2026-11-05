// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The exact upstream authority; URI normalization never changes identity.</summary>
public record UpstreamIssuer : ConceptAs<string>
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamIssuer"/> record.</summary>
    /// <param name="value">An absolute authority URI.</param>
    public UpstreamIssuer(string value) : base(value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _) || value.Any(char.IsControl))
        {
            throw new InvalidAccountValue(nameof(UpstreamIssuer));
        }
    }

    /// <summary>Converts an exact authority URI.</summary>
    /// <param name="value">The URI.</param>
    public static implicit operator UpstreamIssuer(string value) => new(value);
}
