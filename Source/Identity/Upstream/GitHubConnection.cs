// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>The GitHub immutable-id adapter; login is never an identity key.</summary>
/// <param name="connectionId">The configured connection.</param>
/// <param name="time">The trusted clock.</param>
public class GitHubConnection(ConnectionId connectionId, TimeProvider time)
{
    /// <summary>Binds a verified provider response to the existing account session's link ceremony.</summary>
    /// <param name="user">The verified provider response.</param>
    /// <param name="session">The authenticated existing-account session.</param>
    /// <param name="operationId">The callback operation reference retained for retries.</param>
    /// <returns>The session-bound new-key proof.</returns>
    public VerifiedAuthentication ForLink(GitHubUser user, ClaimsPrincipal session, FactId operationId)
    {
        var existing = AccountSession.Require(session, time);
        var verified = From(user, operationId);
        return new(verified.ConnectionId, verified.IdpIssuer, verified.Subject, verified.AuthTime, operationId) { SessionId = existing.SessionId };
    }

    /// <summary>Vouches for a positive numeric GitHub id from the verified provider response.</summary>
    /// <param name="user">The upstream user response.</param>
    /// <param name="operationId">The callback operation reference retained for retries.</param>
    /// <returns>The verified immutable pair.</returns>
    /// <exception cref="AccountOperationRefused">The provider did not supply a positive immutable id.</exception>
    public VerifiedAuthentication From(GitHubUser user, FactId operationId)
    {
        if (user.Id <= 0)
        {
            throw new AccountOperationRefused("invalid_upstream_subject");
        }
        return new(connectionId, "https://github.com", user.Id.ToString(CultureInfo.InvariantCulture), time.GetUtcNow(), operationId);
    }
}
