// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_concurrent_commands_append_to_the_shared_log : given.concurrent_commands
{
    bool _firstStillWaiting;

    async Task Because()
    {
        _appends.OnNext([new(null!, _firstAppend), new(null!, _secondAppend)]);
        _firstPipeline.SetResult(new() { CorrelationId = _firstAppend.CorrelationId });
        _secondPipeline.SetResult(new() { CorrelationId = _secondAppend.CorrelationId });
        await Task.WhenAll(_firstWait.Task, _secondWait.Task).WaitAsync(TimeSpan.FromSeconds(5));
        _secondCompletion.SetResult(new(true, []));
        await _secondExecution.WaitAsync(TimeSpan.FromSeconds(5));
        _firstStillWaiting = !_firstExecution.IsCompleted;
        _firstCompletion.SetResult(new(true, []));
        await _firstExecution.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_let_a_foreign_append_satisfy_the_first_wait() => _firstStillWaiting.ShouldBeTrue();
    [Fact] void should_not_let_the_first_append_block_the_second_wait() => _secondExecution.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_wait_for_the_first_append_only_once() => _progress.Received(1).Complete(_firstAppend, Arg.Any<TimeSpan>());
    [Fact] void should_wait_for_the_second_append_only_once() => _progress.Received(1).Complete(_secondAppend, Arg.Any<TimeSpan>());
    [Fact] void should_dispose_both_captures() => _appends.HasObservers.ShouldBeFalse();
}
