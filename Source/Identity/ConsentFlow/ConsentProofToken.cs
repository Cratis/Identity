// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.ConsentFlow;

/// <summary>A short-lived protected consent decision token.</summary>
/// <param name="Value">The protected token.</param>
internal sealed record ConsentProofToken(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive protected proof.</summary>
    /// <param name="value">The protected proof.</param>
    public static implicit operator ConsentProofToken(string value) => new(value);

    /// <summary>Preserves an optional submitted proof at the command boundary.</summary>
    /// <param name="value">The submitted proof, or null.</param>
    /// <returns>The protected token, or null when no proof was supplied.</returns>
    internal static ConsentProofToken? From(string? value) => value is null ? null : new(value);
}
