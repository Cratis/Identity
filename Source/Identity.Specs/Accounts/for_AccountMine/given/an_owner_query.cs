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

namespace Cratis.Identity.Accounts.for_AccountMine.given;

public class an_owner_query : Specification
{
    protected AccountSubject _subject;
    protected Account _own;
    protected Account _other;
    protected QueryResult _result;
    protected AccountSubject _readSubject;
    protected IAccountReader _reader;
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
        _reader = Substitute.For<IAccountReader>();
        _reader.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _readSubject = call.Arg<AccountSubject>();
            return Task.FromResult<Account?>(_readSubject == _subject ? _own : _other);
        });
        _reader.ClearReceivedCalls();
        _scenario.Services.AddSingleton(_reader);
        _scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(new Accounts.given.FixedAccountPrincipal(principal));
    }

    protected async Task Perform() => _result = await _scenario.Perform(nameof(Account.Mine), new QueryArguments { ["accountId"] = _other.Id.Value });

    void Destroy() => _scenario.Dispose();
}
