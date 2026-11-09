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

public class when_a_command_succeeds_with_an_active_request : Specification
{
    ServiceProvider _services;
    Subject<IEnumerable<AppendedEventWithResult>> _appends;
    CancellationTokenSource _request;
    CommandResult _result;

    void Establish()
    {
        _appends = new();
        _request = new();
        var log = Substitute.For<IEventLog>();
        log.AppendOperations.Returns(_appends);
        var progress = Substitute.For<IObserverProgress>();
        progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(5))));
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
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(Task.FromResult(new CommandResult()));
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        _result = await boundary.Execute(new object(), null, _request.Token);
    }

    void Destroy()
    {
        _request.Dispose();
        _services.Dispose();
        _appends.Dispose();
    }

    [Fact] void should_complete_the_command_successfully() => _result.IsSuccess.ShouldBeTrue();
}
