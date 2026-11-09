// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization;

/// <summary>Maps the operational authorization to the existing non-secret grant UUID.</summary>
internal static class CredentialId
{
    /// <summary>The advisory access-token claim; introspection remains authoritative.</summary>
    internal const string ClaimType = "cid";

    /// <summary>Derives the same identifier used by the grant outbox, or none for an unbound token.</summary>
    /// <param name="authorizationId">The validated token's operational authorization.</param>
    /// <returns>The non-secret UUID, never a token or client identifier.</returns>
    internal static string? For(string? authorizationId) => string.IsNullOrEmpty(authorizationId) ? null : OutboxEntry.Source(authorizationId).ToString("D");
}
