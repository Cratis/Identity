// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_subject_token_expires_soon : given.a_token_exchange_request
{
    TimeSpan? _lifetime;

    void Establish()
    {
        _options.Tokens.AccessTokenLifetime = TimeSpan.FromMinutes(5);
        _subject = _subject with { Expiry = _time.Now.AddSeconds(20) };
        _validator.Validate(default!, default!, default).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
    }

    async Task Because()
    {
        await Handle();
        _lifetime = Issued.GetAccessTokenLifetime();
    }

    [Fact] void should_issue_a_token_that_lives_no_longer_than_the_subject_token() => _lifetime.ShouldEqual(TimeSpan.FromSeconds(20));
}
