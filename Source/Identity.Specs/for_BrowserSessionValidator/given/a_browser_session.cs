// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.for_RefreshGrantAdmission.given;

namespace Cratis.Identity.for_BrowserSessionValidator.given;

public class a_browser_session : Specification
{
    internal static readonly Guid Link = a_refresh_admission.DirectoryLink;

    internal IBrowserSessionStore _sessions;
    internal IAccountReader _accounts;
    internal BrowserSessionValidator _validator;
    internal Account _account;
    internal ClaimsPrincipal _principal;
    internal string _sessionId;
    internal bool _result;

    void Establish()
    {
        _sessions = Substitute.For<IBrowserSessionStore>();
        _accounts = Substitute.For<IAccountReader>();
        _validator = new(_sessions, _accounts);
        _account = a_refresh_admission.AccountWithLinks(Link);
        _principal = SessionPrincipal.For(_account, "GitHub", DateTimeOffset.UnixEpoch, Link);
        _sessionId = SessionPrincipal.SessionId(_principal);
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(_account);
    }

    internal void Recorded(DateTimeOffset? revokedAt = null, Guid? link = null) =>
        _sessions.Find(_sessionId, Arg.Any<CancellationToken>()).Returns(new BrowserSession(_sessionId, _account.Id.Value, link ?? Link, DateTimeOffset.UnixEpoch, revokedAt));
}
