// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Decides whether a session cookie principal may still be used. Fails closed: a cookie without a session identifier or
/// link, without a standing record, or whose link is no longer one of the account's links is refused, and so is any
/// failure to find out.
/// </summary>
/// <param name="scopes">The factory for realm-bound session and account readers.</param>
internal class BrowserSessionValidator(IServiceScopeFactory scopes)
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
            using var realm = IdentityRealmScope.Begin();
            await using var child = scopes.CreateAsyncScope();
            var sessions = child.ServiceProvider.GetRequiredService<IBrowserSessionStore>();
            var session = await sessions.Find(sessionId, cancellationToken);
            return Stands(session, link.Value, subject!) && await IsLinked(child.ServiceProvider.GetRequiredService<IAccountReader>(), subject!, session!.LinkId, cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool Stands(BrowserSession? session, Guid link, AccountSubject subject) =>
        session is { RevokedAt: null } && session.LinkId == link && session.Subject == subject.Value;

    static async Task<bool> IsLinked(IAccountReader accounts, AccountSubject subject, Guid link, CancellationToken cancellationToken)
    {
        var account = await accounts.FindActive(subject, cancellationToken);
        return account is not null && AuthenticatingLink.IsCurrent(account, link);
    }
}
