// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Evaluates OIDC session age against the original upstream authentication, never cookie renewal.</summary>
internal static class OidcAuthentication
{
    /// <summary>Checks max_age using the same integer-second precision as auth_time.</summary>
    /// <param name="request">The validated authorization request.</param>
    /// <param name="authTime">The original upstream authentication time.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>Whether authentication satisfies the request.</returns>
    public static bool IsFresh(OpenIddictRequest request, DateTimeOffset authTime, DateTimeOffset now) =>
        !request.HasScope(Scopes.OpenId) || request.MaxAge is null || now.ToUnixTimeSeconds() - authTime.ToUnixTimeSeconds() <= request.MaxAge.Value;
}
