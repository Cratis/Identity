// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.for_SignedInAccounts;

public class when_the_session_has_a_malformed_authentication_time_and_max_age_is_requested : given.a_session
{
    void Establish()
    {
        SetAuthenticationTime("abc");
        _request.MaxAge = 600;
    }

    async Task Because() => _result = await _finder.Find(_http, _request);

    [Fact] void should_require_a_new_sign_in() => _result.ShouldBeNull();
    [Fact] async Task should_not_look_up_the_account() => await _accounts.DidNotReceive().FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>());
}
