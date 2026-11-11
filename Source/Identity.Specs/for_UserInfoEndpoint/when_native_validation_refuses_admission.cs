// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_UserInfoEndpoint;

public class when_native_validation_refuses_admission : given.a_userinfo_request
{
    protected override bool AuthenticationSucceeds => false;

    Task Because() => ReadUserInfo();

    [Fact] void should_fail_closed() => ((IStatusCodeHttpResult)_result).StatusCode.ShouldEqual(401);
    [Fact] Task should_not_read_profile_data() => _reader.DidNotReceive().FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>());
}
