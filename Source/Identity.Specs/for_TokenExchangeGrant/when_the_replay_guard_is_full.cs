// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_replay_guard_is_full : given.a_token_exchange_request
{
    void Establish()
    {
        for (var i = 0; i < SubjectTokenReplayGuard.MaximumEntries; i++)
        {
            _replay.TryRecord($"entry-{i}", _time.Now.AddHours(1));
        }
    }

    async Task Because() => await Handle();

    [Fact] void should_not_issue_a_token() => IsError.ShouldBeTrue();
    [Fact] void should_report_temporarily_unavailable_not_a_replay() => Error.ShouldEqual(Errors.TemporarilyUnavailable);
}
