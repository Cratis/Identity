// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Realm.for_IdentityRealmNamespaceFilter;

public class when_the_event_store_is_not_default : Specification
{
    ServiceProvider _services;
    CommandResult _command;
    QueryResult _query;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.Namespace.Returns(new EventStoreNamespaceName("tenant-a"));
        _services = new ServiceCollection().AddSingleton(store).BuildServiceProvider();
    }

    async Task Because()
    {
        var filter = new IdentityRealmNamespaceFilter(NullLogger<IdentityRealmNamespaceFilter>.Instance);
        _command = await filter.OnExecution(new(CorrelationId.New(), GetType(), this, [], new(), ServiceProvider: _services));
        _query = await filter.OnPerform(new("Cratis.Identity.Accounts.Account.Mine", CorrelationId.New(), Paging.NotPaged, Sorting.None, ServiceProvider: _services));
    }

    [Fact] void should_refuse_the_command() => _command.ExceptionMessages.ShouldContain(new IdentityRealmNamespaceMismatch().Message);
    [Fact] void should_refuse_the_query() => _query.ExceptionMessages.ShouldContain(new IdentityRealmNamespaceMismatch().Message);

    void Destroy() => _services.Dispose();
}
