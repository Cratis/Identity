// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Realm;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_the_pipeline_throws : Specification
{
    ServiceProvider _services;
    IDisposable _dependency;
    Exception _error;

    void Establish()
    {
        _dependency = Substitute.For<IDisposable>();
        _services = new ServiceCollection().AddScoped(_ => _dependency).BuildServiceProvider();
    }

    async Task Because()
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException("expected"));
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        try
        {
            await boundary.Execute(new object(), provider =>
            {
                ProtocolCommandScope.IsActive.ShouldBeTrue();
                IdentityRealmScope.IsActive.ShouldBeTrue();
                provider.GetRequiredService<IDisposable>().ShouldEqual(_dependency);
            });
        }
        catch (InvalidOperationException exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_propagate_the_error() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_dispose_the_child_scope() => _dependency.Received(1).Dispose();
    [Fact] void should_restore_protocol_state() => ProtocolCommandScope.IsActive.ShouldBeFalse();
    [Fact] void should_restore_realm_state() => IdentityRealmScope.IsActive.ShouldBeFalse();

    void Destroy() => _services.Dispose();
}
