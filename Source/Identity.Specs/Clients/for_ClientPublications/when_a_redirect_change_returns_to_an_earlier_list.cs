// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Clients.Publishing;
using Cratis.Identity.Contracts.Clients.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Clients.for_ClientPublications;

public class when_a_redirect_change_returns_to_an_earlier_list : Specification
{
    readonly EventStoreForTesting _store = new();
    readonly EventSourceId _client = Guid.NewGuid();
    AppendedEvent[] _published;

    async Task Because()
    {
        var publisher = new ClientPublications(_store, NullLogger<ClientPublications>.Instance);
        string[][] lists = [["http://127.0.0.1/a"], ["http://127.0.0.1/b"], ["http://127.0.0.1/a"]];
        var sequence = 1ul;
        foreach (var list in lists)
        {
            var context = EventContext.EmptyWithEventSourceId(_client) with { SequenceNumber = sequence, Occurred = DateTimeOffset.UnixEpoch.AddMinutes(sequence++) };
            await publisher.Publish(new ClientRedirectUrisChanged(list), context);
        }

        _published = (await _store.GetEventSequence(EventSequenceId.Outbox).GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
    }

    [Fact] void should_publish_all_three_changes() => _published.Length.ShouldEqual(3);
    [Fact] void should_publish_the_return_to_the_earlier_list() => ((RedirectUrisChangedForClient)_published[2].Content).RedirectUris.ShouldEqual(["http://127.0.0.1/a"]);
    void Destroy() => _store.Dispose();
}
