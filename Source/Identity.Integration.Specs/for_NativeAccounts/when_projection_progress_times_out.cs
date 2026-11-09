// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_projection_progress_times_out(IdentityHost host) : IAsyncLifetime
{
    string _reason = string.Empty;
    Account? _sessionAccount;
    EventSequenceNumber _before;
    EventSequenceNumber _after;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>());
        var user = new GitHubUser(given.UpstreamUserIds.Next(), "progress-timeout", "Before Timeout");
        var initial = await adapter.SignIn(connection.From(user, FactId.New()), new(null, "Before Timeout", "progress-timeout"), TestContext.Current.CancellationToken);
        var store = provider.GetRequiredService<IEventStore>();
        var position = await provider.GetRequiredService<IObserverProgress>().Position(typeof(Account));
        host.ProjectionProgress.Freeze(typeof(Account), position);
        try
        {
            var append = await store.EventLog.Append(initial.Id.Value, new AccountNameChanged("External Observation"));
            append.IsSuccess.ShouldBeTrue();
            _before = await store.EventLog.GetTailSequenceNumber(eventSourceId: initial.Id.Value);
            try
            {
                _sessionAccount = await adapter.SignIn(connection.From(user, FactId.New()), new(null, "Must Not Append", "progress-timeout"), TestContext.Current.CancellationToken);
            }
            catch (AccountOperationRefused refusal)
            {
                _reason = refusal.Message;
            }

            _after = await store.EventLog.GetTailSequenceNumber(eventSourceId: initial.Id.Value);
        }
        finally
        {
            host.ProjectionProgress.Release();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_fail_as_temporarily_unavailable() => _reason.ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_return_an_account_for_session_creation() => _sessionAccount.ShouldBeNull();
    [Fact] void should_append_no_events_from_the_timed_out_decision() => _after.ShouldEqual(_before);
}
