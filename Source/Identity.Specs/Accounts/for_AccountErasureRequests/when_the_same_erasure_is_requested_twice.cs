// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureRequests;

public class when_the_same_erasure_is_requested_twice : given.an_erasure_workflow
{
    bool _first;
    bool _second;
    string? _other;

    async Task Establish() => await Tombstone();

    async Task Because()
    {
        _first = await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _second = await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
        _other = await Refusal(() => Requests.Request(new ClaimsPrincipal(), _account, ErasureId.New()));
    }

    [Fact] void should_accept_the_first_request() => _first.ShouldBeTrue();
    [Fact] void should_treat_the_repeat_as_a_no_op() => _second.ShouldBeFalse();
    [Fact] void should_append_one_request_fact() => _log.Facts.OfType<AccountErasureRequested>().Count().ShouldEqual(1);
    [Fact] void should_refuse_a_different_erasure() => _other.ShouldEqual("erasure_already_requested");
}
