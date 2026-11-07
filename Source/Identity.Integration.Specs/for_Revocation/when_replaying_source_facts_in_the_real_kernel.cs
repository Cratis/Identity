// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_Revocation;

[Collection(IdentityCollection.Name)]
public class when_replaying_source_facts_in_the_real_kernel(IdentityHost host) : IAsyncLifetime
{
    int _count;
    bool _supportsKeyedAppend;
    Exception _conflict;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var sequence = eventStore.GetEventSequence(EventSequenceId.Log);
        _supportsKeyedAppend = sequence.SupportsIdempotentAppends;
        var grant = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, grant, Guid.NewGuid().ToString("D"), now, now.AddDays(30));
        await new ChronicleRevocationFactWriter(eventStore).Append(entry, CancellationToken.None);

        // A fresh writer has no delivery memory: only the real kernel can deduplicate this replay.
        await new ChronicleRevocationFactWriter(eventStore).Append(entry, CancellationToken.None);
        var events = await sequence.GetFromSequenceNumber(EventSequenceNumber.First, new EventSourceId(entry.GrantId.ToString("D")));
        _count = events.Count;
        _conflict = await Catch.Exception(() => new ChronicleRevocationFactWriter(eventStore).Append(entry with { ExpiresAt = now.AddDays(31) }, CancellationToken.None));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_require_actual_kernel_keyed_support() => _supportsKeyedAppend.ShouldBeTrue();
    [Fact] void should_store_exactly_one_fact_across_fresh_writers() => _count.ShouldEqual(1);
    [Fact] void should_reject_changed_content_for_the_same_fact_key() => _conflict.ShouldBeOfExactType<OutboxContentConflict>();
}
