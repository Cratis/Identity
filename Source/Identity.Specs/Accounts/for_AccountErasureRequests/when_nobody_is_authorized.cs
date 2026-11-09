// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureRequests;

public class when_nobody_is_authorized : given.an_erasure_workflow
{
    string? _refusal;
    bool _defaultAllows;

    async Task Establish() => await Tombstone();

    async Task Because()
    {
        _authorized = false;
        _refusal = await Refusal(() => Requests.Request(new ClaimsPrincipal(), _account, _erasure));
        _defaultAllows = await new DenyAllErasureAuthorization().IsAllowed(new ClaimsPrincipal(), _account);
    }

    [Fact] void should_refuse_the_request() => _refusal.ShouldEqual("erasure_not_authorized");
    [Fact] void should_append_nothing() => _log.Facts.Count(fact => fact is not AccountTombstoneRecorded).ShouldEqual(0);
    [Fact] void should_deny_by_default() => _defaultAllows.ShouldBeFalse();
}
