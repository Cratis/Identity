// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Identity.Configuration;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_request_is_cancelled_during_the_catch_up_wait : given.an_account_adapter
{
    ServiceProvider _services;
    Subject<IEnumerable<AppendedEventWithResult>> _appends;
    IEventLog _log;
    CancellationTokenSource _request;
    TaskCompletionSource _waiting;
    Exception _error;

    void Establish()
    {
        _appends = new();
        _waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _log = Substitute.For<IEventLog>();
        _log.AppendOperations.Returns(_appends);
        var progress = Substitute.For<IObserverProgress>();
        progress.Tail(Arg.Any<Type>()).Returns(Task.FromResult(new EventSequenceNumber(5)));
        progress.Position(Arg.Any<Type>()).Returns(_ =>
        {
            _waiting.TrySetResult();
            return Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(1)));
        });
        _services = new ServiceCollection()
            .AddSingleton(_log)
            .AddSingleton(progress)
            .AddSingleton(Options.Create(new IdentityOptions { Accounts = new() { ReadModelCatchUpTimeout = TimeSpan.FromSeconds(20) } }))
            .AddSingleton(TimeProvider.System)
            .AddScoped<ReadModelCatchUp>()
            .AddScoped<VerifiedUpstreamSignIn>()
            .BuildServiceProvider();
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(Task.FromResult(new CommandResult()));
        _adapter = new(new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline), new(_readModels));
        _request = new();
    }

    async Task Because()
    {
        var signIn = _adapter.SignIn(_verified.Authentication, _verified.Profile, _request.Token);
        await _waiting.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, TestContext.Current.CancellationToken);
        await _request.CancelAsync();
        _error = await Catch.Exception(() => signIn.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System, TestContext.Current.CancellationToken));
    }

    void Destroy()
    {
        _request.Dispose();
        _services.Dispose();
        _appends.Dispose();
    }

    [Fact] void should_stop_waiting_with_the_request_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_append_any_event() => _log.ReceivedCalls().Any(call => call.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal) && !call.GetMethodInfo().Name.StartsWith("AppendOperations", StringComparison.Ordinal)).ShouldBeFalse();
}
