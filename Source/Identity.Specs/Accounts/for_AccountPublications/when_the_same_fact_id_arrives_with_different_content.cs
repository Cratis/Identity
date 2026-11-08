// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Replay.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Accounts.for_AccountPublications;

public class when_the_same_fact_id_arrives_with_different_content : Specification
{
    readonly PublicationReplay _replay = new();
    Exception? _failure;
    int _count;

    async Task Because()
    {
        var fact = (AccountRegistrationRecorded)_replay.Facts[0];
        var publisher = new AccountPublications(_replay.Store, NullLogger<AccountPublications>.Instance);
        var context = EventContext.EmptyWithEventSourceId(_replay.Account) with { Occurred = DateTimeOffset.UnixEpoch };
        await publisher.Publish(fact, context);
        _failure = await Catch.Exception(() => publisher.Publish(fact with { RegisteredAt = DateTimeOffset.UnixEpoch.AddDays(1) }, context));
        _count = (await _replay.Published()).Length;
    }

    [Fact] void should_hold_conflicting_content() => _failure.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_preserve_the_failure_reason() => _failure.Message.ShouldEqual("account_publication_failed");
    [Fact] void should_not_append_a_second_contract() => _count.ShouldEqual(1);
    void Destroy() => _replay.Dispose();
}
