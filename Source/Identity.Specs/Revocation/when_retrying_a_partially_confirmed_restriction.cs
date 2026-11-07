// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.RevocationSpecs;

public class when_retrying_a_partially_confirmed_restriction : Specification
{
    SharedFenceStore _store;
    GrantFence _original;
    GrantState _before;
    GrantState _after;
    Exception? _first;

    async Task Establish()
    {
        _store = new SharedFenceStore { FailConfirmation = true };
        await _store.RecordFamilyStart("grant", "presented", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
    }

    async Task Because()
    {
        GrantRevocation Replica() => new(Substitute.For<IOpenIddictTokenManager>(), Substitute.For<IOpenIddictAuthorizationManager>(), _store, FenceDoubles.Confirmation(_store));
        _first = await Catch.Exception(() => Replica().RevokeAfterReuse("grant", CancellationToken.None));
        _before = await _store.Read("grant", CancellationToken.None);
        _original = _before.Fence;
        _store.FailConfirmation = false;
        await Replica().RevokeAfterReuse("grant", CancellationToken.None);
        await Replica().RevokeAfterReuse("grant", CancellationToken.None);
        _after = await _store.Read("grant", CancellationToken.None);
    }

    [Fact] void should_not_acknowledge_a_failed_confirmation() => _first.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reuse_the_same_sequence_and_identity() => _after.Fence.ShouldEqual(_original);
    [Fact] void should_not_duplicate_any_source_intents() => _after.Outbox.Length.ShouldEqual(4);
    [Fact] void should_reuse_the_exact_fact_and_detection_identities() => _after.Outbox.Select(entry => (entry.Id, entry.DetectionId)).ShouldEqual(_before.Outbox.Select(entry => (entry.Id, entry.DetectionId)));
}
