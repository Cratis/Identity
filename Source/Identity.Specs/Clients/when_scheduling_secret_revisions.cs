// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Clients.RotatingSecret;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

public class when_scheduling_secret_revisions : given.client_commands
{
    [Fact]
    void should_keep_the_previous_digest_until_a_zero_overlap_activation()
    {
        var reducer = new ClientRegistrationReducer();
        var activation = Now.AddHours(1);
        var nextHash = _digester.Digest(Plaintext);
        var rotated = reducer.On(new ClientSecretRotated(ClientSecretId.New(), nextHash, activation, activation), _active, EventContext.EmptyWithEventSourceId(_id.Value));
        rotated.PreviousSecretHash.ShouldEqual(_active.SecretHash);
        rotated.ValidSecretHashesAt(activation.AddTicks(-1)).ShouldEqual([_active.SecretHash]);
        rotated.ValidSecretHashesAt(activation).ShouldEqual([nextHash]);
    }

    [Fact]
    void should_preserve_every_still_valid_revision_across_successive_scheduled_rotations()
    {
        var reducer = new ClientRegistrationReducer();
        var context = EventContext.EmptyWithEventSourceId(_id.Value);
        var firstActivation = Now.AddHours(1);
        var secondActivation = Now.AddHours(2);
        var firstHash = _digester.Digest(Plaintext);
        var secondHash = _digester.Digest("synthetic-second-replacement-secret");
        var first = reducer.On(new ClientSecretRotated(ClientSecretId.New(), firstHash, firstActivation, firstActivation), _active, context);
        var second = reducer.On(new ClientSecretRotated(ClientSecretId.New(), secondHash, secondActivation, secondActivation), first, context);
        second.ValidSecretHashesAt(firstActivation.AddTicks(-1)).ShouldEqual([_active.SecretHash]);
        second.ValidSecretHashesAt(firstActivation).ShouldEqual([firstHash]);
        second.ValidSecretHashesAt(secondActivation.AddTicks(-1)).ShouldEqual([firstHash]);
        second.ValidSecretHashesAt(secondActivation).ShouldEqual([secondHash]);
        second.PreviousSecretRevisions.Length.ShouldEqual(2);
    }

    [Fact]
    void should_retain_an_older_overlap_until_its_own_deadline()
    {
        var reducer = new ClientRegistrationReducer();
        var context = EventContext.EmptyWithEventSourceId(_id.Value);
        var firstHash = _digester.Digest(Plaintext);
        var secondHash = _digester.Digest("synthetic-second-replacement-secret");
        var deadline = Now.AddHours(3);
        var first = reducer.On(new ClientSecretRotated(ClientSecretId.New(), firstHash, Now.AddHours(1), deadline), _active, context);
        var second = reducer.On(new ClientSecretRotated(ClientSecretId.New(), secondHash, Now.AddHours(2), Now.AddHours(2)), first, context);
        second.ValidSecretHashesAt(Now.AddHours(2)).ShouldEqual([_active.SecretHash, secondHash]);
        second.ValidSecretHashesAt(deadline).ShouldEqual([secondHash]);
    }

    [Fact]
    void should_refuse_reuse_of_the_effective_digest_while_a_new_revision_is_pending()
    {
        var reducer = new ClientRegistrationReducer();
        var effective = _active with { SecretHash = _digester.Digest(Plaintext) };
        var activation = Now.AddHours(1);
        var pending = reducer.On(new ClientSecretRotated(ClientSecretId.New(), _active.SecretHash, activation, activation), effective, EventContext.EmptyWithEventSourceId(_id.Value));
        new RotateSecret(_id, Plaintext, new(activation, activation)).Handle(pending, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_clear_all_digest_revisions_on_retirement_and_never_restore_them()
    {
        var reducer = new ClientRegistrationReducer();
        var context = EventContext.EmptyWithEventSourceId(_id.Value);
        var rotation = new ClientSecretRotated(ClientSecretId.New(), _digester.Digest(Plaintext), Now.AddHours(1), Now.AddHours(2));
        var scheduled = reducer.On(rotation, _active, context);
        var retired = reducer.On(new ClientRetired("owner_retired", Now), scheduled, context);
        retired.SecretHash.ShouldBeNull();
        retired.PreviousSecretHash.ShouldBeNull();
        retired.PreviousSecretRevisions.ShouldBeEmpty();
        retired.ValidSecretHashesAt(Now.AddHours(1)).ShouldBeEmpty();
        reducer.On(rotation, retired, context).ShouldEqual(retired);
    }
}
