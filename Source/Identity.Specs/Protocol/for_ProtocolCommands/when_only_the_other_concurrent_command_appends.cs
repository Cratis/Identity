// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_only_the_other_concurrent_command_appends : given.concurrent_commands
{
    bool _secondStillWaiting;

    async Task Because()
    {
        _appends.OnNext([new(null!, _secondAppend)]);
        _firstPipeline.SetResult(new() { CorrelationId = _firstAppend.CorrelationId });
        _secondPipeline.SetResult(new() { CorrelationId = _secondAppend.CorrelationId });
        await _secondWait.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _firstExecution.WaitAsync(TimeSpan.FromSeconds(5));
        _secondStillWaiting = !_secondExecution.IsCompleted;
        _secondCompletion.SetResult(new(true, []));
        await _secondExecution.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_wait_for_another_commands_append() => _firstExecution.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_acknowledge_the_other_command_early() => _secondStillWaiting.ShouldBeTrue();
    [Fact] void should_only_wait_on_behalf_of_the_appending_command() => _progress.Received(1).Complete(Arg.Any<AppendResult>(), Arg.Any<TimeSpan>());
}
