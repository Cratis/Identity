// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_rotating_a_family_with_an_authenticating_link : Specification
{
    static readonly Guid _link = Guid.Parse("55555555-5555-4555-8555-555555555555");
    SharedFenceStore _store;
    RedeemOutcome _outcome;
    GrantState _successor;

    async Task Because()
    {
        _store = new();
        var initial = await _store.RecordFamilyStart(new FamilyStart("grant", "first", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), _link), CancellationToken.None);
        _outcome = await _store.RedeemAndInstall("grant", "first", "second", initial.Version, CancellationToken.None);
        _successor = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_install_the_successor() => _outcome.ShouldEqual(RedeemOutcome.Installed);
    [Fact] void should_make_the_successor_current() => _successor.CurrentRefreshTokenId.ShouldEqual("second");
    [Fact] void should_carry_the_same_link_on_the_successor_state() => _successor.LinkId.ShouldEqual(_link);
}
