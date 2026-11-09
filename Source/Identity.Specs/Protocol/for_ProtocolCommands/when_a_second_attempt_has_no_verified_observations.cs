// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Protocol.for_ProtocolCommands;

public class when_a_second_attempt_has_no_verified_observations : Specification
{
    ServiceProvider _services;
    VerifiedUpstreamSignIn _first;
    VerifiedUpstreamSignIn _second;
    VerifiedAuthentication _observedOnSecondAttempt;

    void Establish() => _services = new ServiceCollection()
        .AddScoped<VerifiedUpstreamSignIn>()
        .AddScoped<IVerifiedUpstreamSignIn>(provider => provider.GetRequiredService<VerifiedUpstreamSignIn>())
        .BuildServiceProvider();

    async Task Because()
    {
        var calls = 0;
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            var holder = call.Arg<IServiceProvider>().GetRequiredService<IVerifiedUpstreamSignIn>();
            ProtocolCommandScope.IsActive.ShouldBeTrue();
            IdentityRealmScope.IsActive.ShouldBeTrue();
            if (++calls == 2)
            {
                _second = (VerifiedUpstreamSignIn)holder;
                _observedOnSecondAttempt = holder.Authentication;
            }

            return Task.FromResult(new CommandResult { ValidationResults = [ValidationResult.Error("refused")] });
        });
        var boundary = new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline);
        await boundary.Execute(new object(), provider =>
        {
            _first = provider.GetRequiredService<VerifiedUpstreamSignIn>();
            _first.Set(new(GitHubConnection.FixedConnectionId, "https://github.com", "42", DateTimeOffset.UtcNow, FactId.New()), new(null, "Person", "login"));
        });
        await boundary.Execute(new object());
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_use_an_independent_holder() => ReferenceEquals(_first, _second).ShouldBeFalse();
    [Fact] void should_not_leak_verified_authentication_after_failure() => _observedOnSecondAttempt.ShouldBeNull();
    [Fact] void should_restore_the_protocol_scope() => ProtocolCommandScope.IsActive.ShouldBeFalse();
    [Fact] void should_restore_the_realm_scope() => IdentityRealmScope.IsActive.ShouldBeFalse();
}
