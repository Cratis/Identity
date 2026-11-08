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

namespace Cratis.Identity.Protocol.for_ProtocolCommands.given;

public class concurrent_commands : Specification
{
    protected Subject<IEnumerable<AppendedEventWithResult>> _appends;
    private protected IObserverProgress _progress;
    protected AppendResult _firstAppend;
    protected AppendResult _secondAppend;
    protected TaskCompletionSource<CommandResult> _firstPipeline;
    protected TaskCompletionSource<CommandResult> _secondPipeline;
    protected TaskCompletionSource<AppendResultWaitForCompletionResult> _firstCompletion;
    protected TaskCompletionSource<AppendResultWaitForCompletionResult> _secondCompletion;
    protected TaskCompletionSource _firstWait;
    protected TaskCompletionSource _secondWait;
    protected Task<CommandResult> _firstExecution;
    protected Task<CommandResult> _secondExecution;
    ServiceProvider _services;

    void Establish()
    {
        _appends = new();
        _firstAppend = AppendResult.Success(Execution.CorrelationId.New(), new(1));
        _secondAppend = AppendResult.Success(Execution.CorrelationId.New(), new(2));
        _firstPipeline = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondPipeline = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstWait = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondWait = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = Substitute.For<IEventLog>();
        log.AppendOperations.Returns(_appends);
        _progress = Substitute.For<IObserverProgress>();
        _progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(5))));
        _progress.Complete(_firstAppend, Arg.Any<TimeSpan>()).Returns(_ =>
        {
            _firstWait.TrySetResult();
            return _firstCompletion.Task;
        });
        _progress.Complete(_secondAppend, Arg.Any<TimeSpan>()).Returns(_ =>
        {
            _secondWait.TrySetResult();
            return _secondCompletion.Task;
        });
        _services = new ServiceCollection()
            .AddSingleton(log)
            .AddSingleton(_progress)
            .AddSingleton(Options.Create(new IdentityOptions()))
            .AddSingleton(TimeProvider.System)
            .AddScoped<ReadModelCatchUp>()
            .BuildServiceProvider();
        var first = new object();
        var second = new object();
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(first, Arg.Any<IServiceProvider>()).Returns(_firstPipeline.Task);
        pipeline.Execute(second, Arg.Any<IServiceProvider>()).Returns(_secondPipeline.Task);
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        _firstExecution = boundary.Execute(first);
        _secondExecution = boundary.Execute(second);
    }

    void Destroy()
    {
        _services.Dispose();
        _appends.Dispose();
    }
}
