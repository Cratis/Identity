// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Identity.Accounts.for_AccountMine.given;

public class an_owner_query : Specification
{
    protected AccountSubject _subject;
    protected Account _own;
    protected Account _other;
    protected QueryResult _result;
    protected FilterDefinition<Account> _filter;
    protected IMongoCollection<Account> _collection;
    QueryScenario<Account> _scenario;

    protected virtual ClaimsPrincipal Principal => new(new ClaimsIdentity([new Claim("sub", _subject.Value)], "spec"));

    void Establish()
    {
        _subject = AccountSubject.New();
        _own = new(_subject, "Owner", null, "12345", "display", AccountStatus.Active, []);
        _other = new(AccountSubject.New(), "Other", null, "54321", "other", AccountStatus.Active, []);
        _scenario = new();
        var store = Substitute.For<IEventStore>();
        store.Namespace.Returns(EventStoreNamespaceName.Default);
        _scenario.Services.AddSingleton(store);
        var principal = Principal;
        _scenario.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        _collection = Substitute.For<IMongoCollection<Account>>();
        var cursor = Substitute.For<IAsyncCursor<Account>>();
        cursor.Current.Returns([_own]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collection.FindAsync(Arg.Any<FilterDefinition<Account>>(), Arg.Any<FindOptions<Account, Account>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _filter = call.ArgAt<FilterDefinition<Account>>(0);
            return Task.FromResult(cursor);
        });
        _collection.ClearReceivedCalls();
        _scenario.Services.AddSingleton(_collection);
        _scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(new Cratis.Identity.Accounts.given.FixedAccountPrincipal(principal));
    }

    protected async Task Perform() => _result = await _scenario.Perform(nameof(Account.Mine), new QueryArguments { ["accountId"] = _other.Id.Value });

    void Destroy() => _scenario.Dispose();
}
