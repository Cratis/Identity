// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Realm;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_the_pipeline_throws : Specification
{
    ServiceProvider _services;
    IDisposable _dependency;
    Exception _error;
    Subject<IEnumerable<AppendedEventWithResult>> _appends;

    void Establish()
    {
        _dependency = Substitute.For<IDisposable>();
        _appends = new();
        var log = Substitute.For<IEventLog>();
        log.AppendOperations.Returns(_appends);
        _services = new ServiceCollection().AddSingleton(log).AddScoped(_ => _dependency).BuildServiceProvider();
    }

    async Task Because()
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException("expected"));
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        try
        {
            void Prepare(IServiceProvider provider)
            {
                ProtocolCommandScope.IsActive.ShouldBeTrue();
                IdentityRealmScope.IsActive.ShouldBeTrue();
                provider.GetRequiredService<IDisposable>().ShouldEqual(_dependency);
            }

            await boundary.Execute(new object(), Prepare, CancellationToken.None);
        }
        catch (InvalidOperationException exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_propagate_the_error() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_dispose_the_child_scope() => _dependency.Received(1).Dispose();
    [Fact] void should_dispose_append_capture() => _appends.HasObservers.ShouldBeFalse();
    [Fact] void should_restore_protocol_state() => ProtocolCommandScope.IsActive.ShouldBeFalse();
    [Fact] void should_restore_realm_state() => IdentityRealmScope.IsActive.ShouldBeFalse();

    void Destroy()
    {
        _services.Dispose();
        _appends.Dispose();
    }
}
