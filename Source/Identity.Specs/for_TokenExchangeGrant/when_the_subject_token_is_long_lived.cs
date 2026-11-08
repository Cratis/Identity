// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_subject_token_is_long_lived : given.a_token_exchange_request
{
    TimeSpan? _lifetime;

    void Establish()
    {
        _options.Tokens.AccessTokenLifetime = TimeSpan.FromMinutes(3);
        _subject = _subject with { Expiry = _time.Now.AddHours(1) };
        _validator.Validate(default!, default!, default).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
    }

    async Task Because()
    {
        await Handle();
        _lifetime = Issued.GetAccessTokenLifetime();
    }

    [Fact] void should_issue_the_configured_lifetime() => _lifetime.ShouldEqual(TimeSpan.FromMinutes(3));
}
