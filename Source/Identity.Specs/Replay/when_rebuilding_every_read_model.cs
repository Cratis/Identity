// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Architecture;
using Cratis.Identity.Replay.given;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.ConsentFlow.Replay;

public class when_rebuilding_every_read_model : given.a_replayed_activation
{
    readonly RevocationReplay _revocation = new();
    int _prepares;
    int _publications;

    async Task Because()
    {
        await Approve();
        Expire();
        await Resume();
        _prepares = counting.Prepares;
        await _revocation.Run();
        using var publications = new PublicationReplay();
        await publications.Replay(1);
        await new ReadModelRebuild().Rebuild();
        await publications.Replay(1000);
        _publications = (await publications.Published()).Length;
        await ReplayTwice();
    }

    [Fact] void should_mint_no_token() => when_inspecting_observers.Observers.SelectMany(when_inspecting_observers.Dependencies).Where(when_inspecting_observers.IsForbidden).ShouldBeEmpty();
    [Fact] void should_send_no_mail() => when_inspecting_observers.Observers.All(when_inspecting_observers.IsClassified).ShouldBeTrue();
    [Fact] void should_not_restore_a_revoked_grant() => _revocation.Redemption.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_not_activate_an_abandoned_or_never_active_consent_grant() => after.State.ShouldEqual(ConsentRequestState.Abandoned);
    [Fact] void should_leave_preparation_untouched() => counting.Prepares.ShouldEqual(_prepares);
    [Fact] void should_only_publish_the_six_keyed_contracts() => _publications.ShouldEqual(6);
}
