// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_AuthenticatingLink;

public class when_stamping_and_reading_the_link : Specification
{
    readonly Guid _link = Guid.NewGuid();
    ClaimsPrincipal _stamped;
    ClaimsPrincipal _unstamped;
    ClaimsPrincipal _session;

    void Because()
    {
        var identity = new ClaimsIdentity();
        AuthenticatingLink.Stamp(identity, _link);
        _stamped = new(identity);
        _unstamped = new(new ClaimsIdentity());
        _session = SessionPrincipal.For(new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Member", null, AccountStatus.Active, []), "test", DateTimeOffset.UnixEpoch, _link);
    }

    [Fact] void should_read_the_stamped_link() => AuthenticatingLink.Of(_stamped).ShouldEqual(_link);
    [Fact] void should_read_no_link_from_an_unstamped_principal() => AuthenticatingLink.Of(_unstamped).ShouldBeNull();
    [Fact] void should_carry_the_link_in_the_session() => AuthenticatingLink.Of(_session).ShouldEqual(_link);
    [Fact] void should_not_give_the_link_a_token_destination() => _stamped.FindFirst(AuthenticatingLink.ClaimType).GetDestinations().ShouldBeEmpty();
}
