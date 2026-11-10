// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Sessions;

/// <summary>The identifier of an authenticated session.</summary>
/// <param name="Value">The session identifier.</param>
internal sealed record SessionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Converts a primitive session identifier.</summary>
    /// <param name="value">The session identifier.</param>
    public static implicit operator SessionId(string value) => new(value);
}
