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

public class when_a_command_succeeds_without_own_appends_while_another_command_appends_and_a_projection_is_stalled : Specification
{
    ServiceProvider _services;
    Subject<IEnumerable<AppendedEventWithResult>> _appends;
    Exception _error;

    void Establish()
    {
        _appends = new();
        var log = Substitute.For<IEventLog>();
        log.AppendOperations.Returns(_appends);
        var progress = Substitute.For<IObserverProgress>();
        progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Suspended, false, false, new(1))));
        _services = new ServiceCollection()
            .AddSingleton(log)
            .AddSingleton(progress)
            .AddSingleton(Options.Create(new IdentityOptions()))
            .AddSingleton(TimeProvider.System)
            .AddScoped<ReadModelCatchUp>()
            .BuildServiceProvider();
    }

    async Task Because()
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(_ =>
        {
            // An unrelated request appends to the shared log while this command runs and appends nothing itself.
            _appends.OnNext([new(null!, AppendResult.Success(Execution.CorrelationId.New(), new(2)))]);
            return Task.FromResult(new CommandResult { CorrelationId = Execution.CorrelationId.New() });
        });
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        _error = await Catch.Exception(() => boundary.Execute(new object()));
    }

    void Destroy()
    {
        _services.Dispose();
        _appends.Dispose();
    }

    [Fact] void should_still_fence_on_the_read_models() => _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
}
