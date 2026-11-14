// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Decides whether a session cookie principal may still be used. Fails closed: a cookie without a session identifier or
/// link, without a standing record, or whose link is no longer one of the account's links is refused, and so is any
/// failure to find out.
/// </summary>
/// <param name="sessions">The <see cref="IBrowserSessionStore"/>.</param>
/// <param name="accounts">The <see cref="IAccountReader"/>.</param>
internal class BrowserSessionValidator(IBrowserSessionStore sessions, IAccountReader accounts)
{
    /// <summary>Checks a session cookie principal.</summary>
    /// <param name="principal">The principal read from the cookie.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether the session may be used.</returns>
    public async Task<bool> IsValid(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionId = SessionPrincipal.SessionId(principal);
        var link = AuthenticatingLink.Of(principal);
        if (string.IsNullOrWhiteSpace(sessionId) || link is null || !AccountSubject.TryParse(SessionPrincipal.AccountId(principal), out var subject))
        {
            return false;
        }

        try
        {
            var session = await sessions.Find(sessionId, cancellationToken);
            if (session is null || session.RevokedAt is not null || session.LinkId != link || session.Subject != subject!.Value)
            {
                return false;
            }

            var account = await accounts.FindActive(subject, cancellationToken);
            return account is not null && AuthenticatingLink.IsCurrent(account, session.LinkId);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
