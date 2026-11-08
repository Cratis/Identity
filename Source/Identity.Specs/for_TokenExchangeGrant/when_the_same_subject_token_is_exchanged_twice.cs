// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_same_subject_token_is_exchanged_twice : given.a_token_exchange_request
{
    IResult _first;

    async Task Because()
    {
        await Handle();
        _first = _result;
        await Handle();
    }

    [Fact] void should_issue_the_first_time() => _first.ShouldBeOfExactType<SignInHttpResult>();
    [Fact] void should_refuse_the_second_time() => Error.ShouldEqual(Errors.InvalidGrant);
}
