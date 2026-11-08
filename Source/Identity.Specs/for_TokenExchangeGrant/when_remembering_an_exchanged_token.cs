// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_remembering_an_exchanged_token : given.a_token_exchange_request
{
    bool _usedWithinAgeAndSkew;
    bool _usedAfterAgeAndSkew;

    void Establish()
    {
        // Issued now, valid for an hour: the guard only needs it until the token is too old to be accepted.
        _subject = _subject with { IssuedAt = _time.Now, Expiry = _time.Now.AddHours(1) };
        _validator.Validate(default!, default!, default).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));
    }

    async Task Because()
    {
        await Handle();
        _time.Now = _time.Now.AddMinutes(10).AddSeconds(30);
        _usedWithinAgeAndSkew = _replay.WasUsed(_subject.ReplayKey);
        _time.Now = _time.Now.AddMinutes(1);
        _usedAfterAgeAndSkew = _replay.WasUsed(_subject.ReplayKey);
    }

    [Fact] void should_remember_it_until_it_is_too_old_to_be_accepted() => _usedWithinAgeAndSkew.ShouldBeTrue();
    [Fact] void should_forget_it_once_it_is_too_old_to_be_accepted() => _usedAfterAgeAndSkew.ShouldBeFalse();
}
