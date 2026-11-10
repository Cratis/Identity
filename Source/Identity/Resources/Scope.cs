// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Resources;

/// <summary>A protocol scope value.</summary>
/// <param name="Value">The scope.</param>
internal sealed record Scope(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive scope.</summary>
    /// <param name="value">The scope.</param>
    public static implicit operator Scope(string value) => new(value);
}
