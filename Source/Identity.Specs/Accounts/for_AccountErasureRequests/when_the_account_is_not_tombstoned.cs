// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Identity.Accounts.for_AccountErasureRequests;

public class when_the_account_is_not_tombstoned : given.an_erasure_workflow
{
    string? _refusal;

    async Task Because() => _refusal = await Refusal(() => Requests.Request(new ClaimsPrincipal(), _account, _erasure));

    [Fact] void should_refuse_until_revoked() => _refusal.ShouldEqual("account_not_revoked");
    [Fact] void should_append_nothing() => _log.Facts.ShouldBeEmpty();
}
