// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard;

public class when_the_caller_cancels : given.a_replay_guard
{
    Exception _exception;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _exception = await Catch.Exception(async () => await Record(cancellation.Token));
    }

    [Fact] void should_preserve_caller_cancellation() => _exception.ShouldBeOfExactType<OperationCanceledException>();
}
