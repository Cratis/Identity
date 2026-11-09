// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_the_request_is_cancelled_while_a_projection_is_catching_up : Specification
{
    ServiceProvider _services;
    Subject<IEnumerable<AppendedEventWithResult>> _appends;
    CancellationTokenSource _request;
    ProtocolCommands _boundary;
    Exception _executeError;
    Exception _retryError;

    void Establish()
    {
        _appends = new();
        var log = Substitute.For<IEventLog>();
        log.AppendOperations.Returns(_appends);
        var progress = Substitute.For<IObserverProgress>();
        progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(1))));
        _services = new ServiceCollection()
            .AddSingleton(log)
            .AddSingleton(progress)
            .AddSingleton(Options.Create(new IdentityOptions()))
            .AddSingleton(TimeProvider.System)
            .AddScoped<ReadModelCatchUp>()
            .BuildServiceProvider();
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(Task.FromResult(new CommandResult()));
        _boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        _request = new();
        _request.Cancel();
    }

    async Task Because()
    {
        _executeError = await Catch.Exception(() => _boundary.Execute(new object(), cancellationToken: _request.Token));
        _retryError = await Catch.Exception(() => _boundary.BeforeRetry(_request.Token));
    }

    void Destroy()
    {
        _request.Dispose();
        _services.Dispose();
        _appends.Dispose();
    }

    [Fact] void should_observe_the_cancellation_while_executing() => (_executeError is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_observe_the_cancellation_before_the_retry() => (_retryError is OperationCanceledException).ShouldBeTrue();
}
