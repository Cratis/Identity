// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_UserInfoEndpoint;

public class when_the_account_is_inactive : given.a_userinfo_request
{
    protected override bool ActiveAccount => false;

    Task Because() => ReadUserInfo();

    [Fact] void should_refuse_the_token() => ((IStatusCodeHttpResult)_result).StatusCode.ShouldEqual(401);
    [Fact] void should_challenge_with_invalid_token() => _context.Response.Headers.WWWAuthenticate.ToString().ShouldEqual("Bearer error=\"invalid_token\"");
    [Fact] void should_prevent_storage() => _context.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
}
