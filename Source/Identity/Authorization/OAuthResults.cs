// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Creates results that hand an OAuth outcome back to OpenIddict, which renders it for the client.
/// </summary>
public static class OAuthResults
{
    /// <summary>
    /// Creates a result that rejects the request with an OAuth error.
    /// </summary>
    /// <param name="error">The OAuth error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    public static IResult Error(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    /// <summary>
    /// Creates a result that asks OpenIddict to issue tokens (or an authorization code) for an identity.
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> to issue for.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    public static IResult Issue(ClaimsIdentity identity) =>
        Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
