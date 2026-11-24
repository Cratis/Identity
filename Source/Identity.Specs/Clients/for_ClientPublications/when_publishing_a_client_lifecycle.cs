// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Clients.Publishing;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Replay.for_EventLog.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Clients.for_ClientPublications;

public class when_publishing_a_client_lifecycle : Specification
{
    readonly EventStoreForTesting _store = new();
    readonly EventSourceId _client = Guid.NewGuid();
    readonly ClientRegistered _registered = (ClientRegistered)EventSeeder.Seed(typeof(ClientRegistered));
    readonly ClientRedirectUrisChanged _redirects = (ClientRedirectUrisChanged)EventSeeder.Seed(typeof(ClientRedirectUrisChanged));
    readonly ClientSecretRotated _rotated = (ClientSecretRotated)EventSeeder.Seed(typeof(ClientSecretRotated));
    readonly ClientRetired _retired = (ClientRetired)EventSeeder.Seed(typeof(ClientRetired));
    AppendedEvent[] _first;
    AppendedEvent[] _second;

    async Task Because()
    {
        await Replay(1);
        _first = await Published();
        await Replay(1000);
        _second = await Published();
    }

    [Fact] void should_publish_each_contract_once_in_source_order() => _first.Select(@event => @event.Content.GetType()).ShouldEqual([typeof(ClientAdded), typeof(RedirectUrisChangedForClient), typeof(SecretRotatedForClient), typeof(ClientPermanentlyRetired)]);
    [Fact] void should_publish_every_contract_on_the_clients_own_stream() => _first.All(@event => @event.Context.EventSourceId == _client).ShouldBeTrue();
    [Fact] void should_not_publish_a_redelivered_fact_twice() => _second.Select(@event => @event.Content.GetType()).ShouldEqual(_first.Select(@event => @event.Content.GetType()));
    [Fact] void should_carry_the_protocol_client_id_as_data_not_as_the_stream() => ((ClientAdded)_first[0].Content).ClientId.ShouldEqual(_registered.ClientId);
    [Fact] void should_publish_the_secret_revision() => ((SecretRotatedForClient)_first[2].Content).ClientSecretId.ShouldEqual(_rotated.ClientSecretId);
    [Fact] void should_not_publish_the_secret_digest() => JsonSerializer.Serialize(_first[2].Content).ShouldNotContain(_rotated.ClientSecretHash.Value);
    void Destroy() => _store.Dispose();

    async Task Replay(ulong firstSequence)
    {
        var publisher = new ClientPublications(_store, NullLogger<ClientPublications>.Instance);
        foreach (var fact in new object[] { _registered, _redirects, _rotated, _retired })
        {
            var context = EventContext.EmptyWithEventSourceId(_client) with { SequenceNumber = firstSequence++, Occurred = DateTimeOffset.UnixEpoch };
            await (Task)typeof(ClientPublications).GetMethod(nameof(ClientPublications.Publish), [fact.GetType(), typeof(EventContext)])!.Invoke(publisher, [fact, context])!;
        }
    }

    async Task<AppendedEvent[]> Published() => (await _store.GetEventSequence(EventSequenceId.Outbox).GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
}
