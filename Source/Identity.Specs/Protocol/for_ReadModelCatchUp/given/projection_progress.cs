// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol.for_ReadModelCatchUp.given;

public class projection_progress : Specification
{
    private protected IObserverProgress _progress;
    private protected ReadModelCatchUp _catchUp;
    protected Exception _error;

    void Establish()
    {
        _progress = Substitute.For<IObserverProgress>();
        _progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(5))));
        _catchUp = new(_progress, Options.Create(new IdentityOptions { Accounts = new() { ReadModelCatchUpTimeout = TimeSpan.FromMilliseconds(100) } }), TimeProvider.System);
    }

    protected async Task Wait()
    {
        try
        {
            await _catchUp.BeforeRetry(CancellationToken.None);
        }
        catch (ReadModelsNotCaughtUp exception)
        {
            _error = exception;
        }
    }
}
