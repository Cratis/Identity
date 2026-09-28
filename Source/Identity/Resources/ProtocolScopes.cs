// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Resources;

/// <summary>
/// Holds the scopes that belong to the protocol rather than to a resource server.
/// </summary>
public static class ProtocolScopes
{
    /// <summary>
    /// Gets all protocol scopes Identity accepts.
    /// </summary>
    public static IEnumerable<string> All { get; } = [Scopes.OfflineAccess];

    /// <summary>
    /// Checks whether a scope is a protocol scope.
    /// </summary>
    /// <param name="scope">The scope name.</param>
    /// <returns><see langword="true"/> if the scope is a protocol scope.</returns>
    public static bool Contains(string scope) => All.Contains(scope, StringComparer.Ordinal);
}
