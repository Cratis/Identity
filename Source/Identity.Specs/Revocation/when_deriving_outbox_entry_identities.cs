// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Events;

namespace Cratis.Identity.RevocationSpecs;

public class when_deriving_outbox_entry_identities : Specification
{
    static readonly Guid _fenceId = new("11111111-1111-4111-8111-111111111111");
    static readonly DateTimeOffset _recordedAt = DateTimeOffset.UnixEpoch;

    GrantFence _fence;
    OutboxEntry[] _first;
    OutboxEntry[] _second;
    OutboxEntry[] _otherSequence;
    OutboxEntry[] _otherGrant;

    void Establish()
    {
        _fence = new(_fenceId, 1, "grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, _recordedAt);
        _first = FenceRecordFactory.Entries(_fence);
    }

    void Because()
    {
        _second = FenceRecordFactory.Entries(_fence);
        _otherSequence = FenceRecordFactory.Entries(_fence with { Sequence = 9 });
        _otherGrant = FenceRecordFactory.Entries(_fence with { GrantId = "another-grant" });
    }

    [Theory]
    [InlineData(FactKind.AuthorizationRevocation, typeof(AuthorizationRevocationRecorded))]
    [InlineData(FactKind.FamilyRevocation, typeof(RefreshTokenFamilyRevocationRecorded))]
    [InlineData(FactKind.FamilyStarted, typeof(RefreshTokenFamilyStarted))]
    [InlineData(FactKind.Reuse, typeof(RefreshTokenReuseDetected))]
    public void should_key_each_kind_by_its_event_type_default_id(FactKind kind, Type fact)
    {
        OutboxEntry.EventTypeId(kind).ShouldEqual(fact.Name);
        fact.GetCustomAttribute<EventTypeAttribute>().Id.Value.ShouldEqual(string.Empty);
    }

    [Fact] async Task should_reject_an_unknown_kind() => (await Catch.Exception(() => Task.FromResult(OutboxEntry.EventTypeId((FactKind)99)))).ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_derive_identical_entries_for_the_same_fence() => _second.ShouldEqual(_first);
    [Fact] void should_use_the_fact_and_type_name_as_the_delivery_key() => _first.All(entry => entry.Id == $"{entry.FactId:D}:{OutboxEntry.EventTypeId(entry.Kind)}").ShouldBeTrue();
    [Fact] void should_have_a_distinct_delivery_key_per_kind() => _first.Select(entry => entry.Id).Distinct().Count().ShouldEqual(3);
    [Fact] void should_have_a_distinct_fact_id_per_kind() => _first.Select(entry => entry.FactId).Distinct().Count().ShouldEqual(3);
    [Fact] void should_not_put_the_fence_id_in_the_type_part() => _first.All(entry => !entry.Id[(entry.Id.IndexOf(':') + 1)..].Contains(_fenceId.ToString("D"), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_use_exactly_one_pair_separator() => _first.All(entry => entry.Id.Count(character => character == ':') == 1).ShouldBeTrue();
    [Fact] void should_derive_from_the_fence_id_and_kind_only() => _otherSequence.Select(entry => entry.Id).ShouldEqual(_first.Select(entry => entry.Id).ToArray());
    [Fact] void should_not_depend_on_the_grant_for_the_derivation() => _otherGrant.Select(entry => entry.FactId).ShouldEqual(_first.Select(entry => entry.FactId).ToArray());
    [Fact] void should_derive_a_detection_id_distinct_from_every_fact_id() => _first.Select(entry => (Guid?)entry.FactId).Contains(_first.Single(entry => entry.Kind == FactKind.Reuse).DetectionId).ShouldBeFalse();
    [Fact] void should_pin_the_authorization_fact_id() => _first.Single(entry => entry.Kind == FactKind.AuthorizationRevocation).FactId.ToString("D").ShouldEqual("2c051475-e71f-fc69-eeda-c2f23014b28b");
    [Fact] void should_pin_the_family_fact_id() => _first.Single(entry => entry.Kind == FactKind.FamilyRevocation).FactId.ToString("D").ShouldEqual("f3958a4e-d464-2409-3b6f-f10873c12495");
    [Fact] void should_pin_the_reuse_fact_id() => _first.Single(entry => entry.Kind == FactKind.Reuse).FactId.ToString("D").ShouldEqual("4f216497-9c54-c478-8b30-4f0e11162298");
    [Fact] void should_pin_the_detection_id() => _first.Single(entry => entry.Kind == FactKind.Reuse).DetectionId.ShouldEqual(new Guid("8c555576-fb19-62d5-c25f-6fa56771356b"));
    [Fact] void should_not_reuse_the_legacy_uuid_derivation() => _first.Any(entry => entry.FactId == new Guid("5c403819-b3eb-9727-7cee-1d7c73cc9371")).ShouldBeFalse();

    [Fact]
    void should_keep_family_start_fact_ids_random_but_the_key_shaped_as_a_pair()
    {
        var one = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, _recordedAt, _recordedAt.AddDays(30));
        var other = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, _recordedAt, _recordedAt.AddDays(30));
        (one.FactId == other.FactId).ShouldBeFalse();
        one.Id.ShouldEqual($"{one.FactId:D}:RefreshTokenFamilyStarted");
    }
}
