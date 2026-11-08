// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_checking_source_fact_schemas : Specification
{
    OutboxEntry[] _entries;
    string[] _payloads;

    async Task Because()
    {
        var store = new SharedFenceStore();
        await store.RecordFamilyStart("operational-authorization", "presented-entry", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        await store.RedeemAndInstall("operational-authorization", "presented-entry", "successor-entry", 1, CancellationToken.None);
        await store.Commit("operational-authorization", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _entries = (await store.Read("operational-authorization", CancellationToken.None)).Outbox;
        _payloads = _entries.Select(entry => JsonSerializer.Serialize(entry.Fact(), entry.Fact().GetType())).ToArray();
    }

    [Fact] void should_cover_all_four_closed_schemas() => _entries.Select(entry => entry.Kind).Distinct().Count().ShouldEqual(4);
    [Fact] void should_keep_all_new_types_internal() => _entries.All(entry => !entry.Fact().GetType().IsPublic).ShouldBeTrue();
    [Fact] void should_not_leak_operational_entry_or_authorization_ids() => _payloads.All(payload => !payload.Contains("presented-entry", StringComparison.Ordinal) && !payload.Contains("successor-entry", StringComparison.Ordinal) && !payload.Contains("operational-authorization", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_use_the_fact_and_event_type_pair_as_the_delivery_key() => _entries.All(entry => entry.Id == $"{entry.FactId:D}:{OutboxEntry.EventTypeId(entry.Kind)}").ShouldBeTrue();
    [Fact] void should_never_use_token_fields_in_the_fact_schema() => _entries.SelectMany(entry => entry.Fact().GetType().GetProperties()).Any(property => property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Handle", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
