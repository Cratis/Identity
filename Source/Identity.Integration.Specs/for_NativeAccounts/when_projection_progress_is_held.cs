// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_projection_progress_is_held(IdentityHost host) : IAsyncLifetime
{
    bool _pendingAtFence;
    Account _completed = null!;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>());
        var user = new GitHubUser(given.UpstreamUserIds.Next(), "progress-hold", "Before Progress");
        var initial = await adapter.SignIn(connection.From(user, FactId.New()), new(null, "Before Progress", "progress-hold"));
        var store = provider.GetRequiredService<IEventStore>();
        var position = await provider.GetRequiredService<IObserverProgress>().Position(typeof(Account));
        host.ProjectionProgress.Freeze(typeof(Account), position);
        try
        {
            var append = await store.EventLog.Append(initial.Id.Value, new AccountNameChanged("External Observation"));
            append.IsSuccess.ShouldBeTrue();
            var attempt = adapter.SignIn(connection.From(user, FactId.New()), new(null, "After Progress", "progress-hold"));
            await host.ProjectionProgress.Observed.WaitAsync(TimeSpan.FromSeconds(5));
            _pendingAtFence = !attempt.IsCompleted;
            host.ProjectionProgress.Release();
            _completed = await attempt.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            host.ProjectionProgress.Release();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_not_acknowledge_a_command_while_its_projection_input_is_pending() => _pendingAtFence.ShouldBeTrue();
    [Fact] void should_return_only_the_caught_up_profile_after_release() => _completed.Name.Value.ShouldEqual("After Progress");
}
