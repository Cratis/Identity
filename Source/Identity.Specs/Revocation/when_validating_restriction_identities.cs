// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_validating_restriction_identities : Specification
{
    static readonly Guid _fenceId = new("11111111-1111-4111-8111-111111111111");
    static readonly Guid _legacyAuthorizationFactId = new("5c403819-b3eb-9727-7cee-1d7c73cc9371");
    static readonly DateTimeOffset _recordedAt = DateTimeOffset.UnixEpoch;
    static readonly GrantFence _fence = new(_fenceId, 1, "grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, _recordedAt);

    [Fact] void should_accept_the_canonical_restrictions() => Rejected(FenceRecordFactory.Entries(_fence)).ShouldBeNull();
    [Fact] void should_reject_a_legacy_derived_fact_id() => Rejected(WithFactId(_legacyAuthorizationFactId)).ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_reject_a_random_fact_id_with_a_consistent_name_suffix() => Rejected(WithFactId(Guid.NewGuid())).ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_reject_an_altered_detection_id_on_a_reuse_entry() => Rejected(Replace(FactKind.Reuse, entry => entry with { DetectionId = Guid.NewGuid() })).ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_reject_a_missing_detection_id_on_a_reuse_entry() => Rejected(Replace(FactKind.Reuse, entry => entry with { DetectionId = null })).ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_reject_a_detection_id_on_a_non_reuse_restriction() => Rejected(Replace(FactKind.FamilyRevocation, entry => entry with { DetectionId = Guid.NewGuid() })).ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_accept_random_fact_ids_for_family_start_entries() => GrantStateValidation.IsKnown(ClearState()).ShouldBeTrue();
    [Fact] void should_reject_a_detection_id_on_a_family_start_entry() => GrantStateValidation.IsKnown(ClearState(entry => entry with { DetectionId = Guid.NewGuid() })).ShouldBeFalse();

    [Fact] Task should_append_nothing_when_a_stored_restriction_has_a_legacy_derived_fact_id() => AssertNothingAppended(Tampered(_legacyAuthorizationFactId));
    [Fact] Task should_append_nothing_when_a_stored_restriction_has_a_random_fact_id() => AssertNothingAppended(Tampered(Guid.NewGuid()));
    [Fact] Task should_append_nothing_when_a_stored_reuse_has_an_altered_detection_id() => AssertNothingAppended(Replace(FactKind.Reuse, entry => entry with { DetectionId = Guid.NewGuid() }));
    [Fact] Task should_append_nothing_when_a_stored_reuse_has_no_detection_id() => AssertNothingAppended(Replace(FactKind.Reuse, entry => entry with { DetectionId = null }));

    static async Task AssertNothingAppended(OutboxEntry[] tampered)
    {
        var entries = tampered.Select(entry => entry with { State = OutboxState.LogConfirmed }).ToArray();
        var state = new GrantState("grant", 1, null, _fence, entries);
        var store = Substitute.For<IFenceStore>();
        var writer = Substitute.For<IRevocationFactWriter>();
        store.Undelivered(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([state]);
        store.Read("grant", Arg.Any<CancellationToken>()).Returns(state);

        var error = await Catch.Exception(() => new DispatchRevocationOutbox(store, FenceDoubles.Confirmation(store), writer).Dispatch(CancellationToken.None));

        error.ShouldBeOfExactType<RevocationPending>();
        await writer.DidNotReceive().Append(Arg.Any<OutboxEntry>(), Arg.Any<CancellationToken>());
    }

    static FenceRecord Record(OutboxEntry[] entries) => new(_fence, entries.Select(entry => entry.Id).ToArray(), entries);

    static Exception? Rejected(OutboxEntry[] entries)
    {
        try
        {
            FenceRecordFactory.Validate(Record(entries));
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    static OutboxEntry[] WithFactId(Guid factId) => Tampered(factId);

    static OutboxEntry[] Tampered(Guid factId) => Replace(FactKind.AuthorizationRevocation, entry => entry with { FactId = factId, Id = $"{factId:D}:{OutboxEntry.EventTypeId(entry.Kind)}" });

    static OutboxEntry[] Replace(FactKind kind, Func<OutboxEntry, OutboxEntry> change) =>
        FenceRecordFactory.Entries(_fence).Select(entry => entry.Kind == kind ? change(entry) : entry).ToArray();

    static GrantState ClearState(Func<OutboxEntry, OutboxEntry>? change = null)
    {
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, _recordedAt, _recordedAt.AddDays(30));
        return new("grant", 1, "refresh", null, [change?.Invoke(entry) ?? entry]);
    }
}
