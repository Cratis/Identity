// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_exchanging_a_valid_subject_token : given.a_token_exchange_request
{
    async Task Because() => await Handle();

    [Fact] void should_issue_a_token() => _result.ShouldBeOfExactType<SignInHttpResult>();
    [Fact] void should_not_be_an_error() => IsError.ShouldBeFalse();
}
