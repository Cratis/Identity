// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.TokenExchange;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_subject_token_is_about_to_expire : given.a_token_exchange_request
{
    TimeSpan? _lifetime;

    void Establish()
    {
        _subject = _subject with { Expiry = _time.Now.AddSeconds(1) };
        _validator.Validate(default!, default!, default).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
    }

    async Task Because()
    {
        await Handle();
        _lifetime = Issued.GetAccessTokenLifetime();
    }

    [Fact] void should_keep_the_minimum_lifetime() => _lifetime.ShouldEqual(AccessTokenIdentity.MinimumExchangeLifetime);
}
