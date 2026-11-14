// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.given;

internal static class GitHubAccounts
{
    internal static Account Create(AccountSubject id, PersonalName name, Email? email, UpstreamSubject gitHubId, Login gitHubLogin, AccountStatus status = AccountStatus.Active) =>
        new(id, name, email, status, [Link(gitHubId, gitHubLogin)]);

    internal static LinkedUpstreamKey Link(UpstreamSubject gitHubId, Login gitHubLogin) =>
        new(new(Guid.NewGuid()), GitHubConnection.FixedConnectionId, GitHubConnection.Issuer, gitHubId, gitHubLogin, DateTimeOffset.UnixEpoch);
}
