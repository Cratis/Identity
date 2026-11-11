// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_OidcAuthentication;

public class when_a_session_is_older_than_max_age : Specification
{
    readonly DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
    readonly OpenIddictRequest _request = new() { Scope = "openid", MaxAge = 30 };

    [Fact] void should_require_new_upstream_authentication() => OidcAuthentication.IsFresh(_request, _now.AddSeconds(-31), _now).ShouldBeFalse();
    [Fact] void should_accept_the_boundary() => OidcAuthentication.IsFresh(_request, _now.AddSeconds(-30), _now).ShouldBeTrue();
    [Fact] void should_not_change_oauth_session_admission() => OidcAuthentication.IsFresh(new OpenIddictRequest { Scope = "direct:read", MaxAge = 0 }, _now.AddHours(-1), _now).ShouldBeTrue();
    [Fact] void should_force_new_authentication_with_zero_max_age() => OidcAuthentication.IsFresh(new OpenIddictRequest { Scope = "openid", MaxAge = 0 }, _now, _now).ShouldBeFalse();
}
