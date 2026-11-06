// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp;

public class when_an_append_observer_does_not_complete : given.projection_progress
{
    async Task Because()
    {
        var result = AppendResult.Success(Cratis.Execution.CorrelationId.New(), new(5));
        _progress.Complete(result, Arg.Any<TimeSpan>()).Returns(Task.FromResult(new AppendResultWaitForCompletionResult(false, [])));
        try
        {
            await _catchUp.AfterCommand([new(null!, result)], CancellationToken.None);
        }
        catch (ReadModelsNotCaughtUp exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_refuse_acknowledgement() => _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
}
