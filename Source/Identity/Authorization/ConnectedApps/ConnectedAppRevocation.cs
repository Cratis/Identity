// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.ConnectedApps;

/// <summary>Checks ownership against operational authority, never against the connected-apps list.</summary>
internal sealed class ConnectedAppRevocation
{
    readonly IOpenIddictAuthorizationManager _authorizations;
    readonly GrantRevocation _revocation;

    /// <summary>Initializes a new instance of the <see cref="ConnectedAppRevocation"/> class.</summary>
    /// <param name="authorizations">The realm-bound authorization manager.</param>
    /// <param name="revocation">The durable whole-grant revocation path.</param>
    internal ConnectedAppRevocation(IOpenIddictAuthorizationManager authorizations, GrantRevocation revocation)
    {
        _authorizations = authorizations;
        _revocation = revocation;
    }

    /// <summary>Refuses non-owned authorizations before entering durable revocation.</summary>
    /// <param name="authorizationId">The authorization identifier.</param>
    /// <param name="subject">The authenticated account subject.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The confirmed revocation.</returns>
    internal async Task Revoke(string authorizationId, string subject, CancellationToken cancellationToken)
    {
        var authorization = await _authorizations.FindByIdAsync(authorizationId, cancellationToken);
        AccountOperationRefused.Require(authorization is not null, "connected_app_not_available");
        var owner = await _authorizations.GetSubjectAsync(authorization!, cancellationToken);
        AccountOperationRefused.Require(string.Equals(owner, subject, StringComparison.Ordinal), "connected_app_not_available");
        await _revocation.Revoke(authorizationId, RevocationReason.Revoked, cancellationToken);
    }
}
