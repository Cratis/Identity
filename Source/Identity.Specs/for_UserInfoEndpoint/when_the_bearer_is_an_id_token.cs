// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_UserInfoEndpoint;

public class when_the_bearer_is_an_id_token : given.a_userinfo_request
{
    protected override string TokenType => OpenIddictConstants.TokenTypeIdentifiers.IdentityToken;

    Task Because() => ReadUserInfo();

    [Fact] void should_refuse_the_wrong_token_type() => ((IStatusCodeHttpResult)_result).StatusCode.ShouldEqual(401);
    [Fact] Task should_not_read_profile_data() => _reader.DidNotReceive().FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>());
}
