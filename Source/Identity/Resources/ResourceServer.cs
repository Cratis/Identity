// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>
/// Represents a resource server tokens can be issued for.
/// </summary>
/// <param name="Audience">The audience written to the <c language="text">aud</c> claim.</param>
/// <param name="DisplayName">The human-readable name shown on the consent page.</param>
/// <param name="Indicators">The normalized RFC 8707 resource indicators that map to the audience.</param>
/// <param name="Scopes">The scopes that belong to the resource server.</param>
public record ResourceServer(string Audience, string DisplayName, IEnumerable<string> Indicators, IEnumerable<ScopeDefinition> Scopes)
{
    /// <summary>
    /// Checks whether a scope belongs to the resource server.
    /// </summary>
    /// <param name="scope">The scope name.</param>
    /// <returns><see langword="true"/> if the scope belongs to the resource server.</returns>
    public bool Owns(string scope) => Scopes.Any(_ => string.Equals(_.Name, scope, StringComparison.Ordinal));
}
