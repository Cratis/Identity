// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Testing.Queries;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.given;

public class a_linked_keys_query : a_link_proposal
{
    protected QueryScenario<LinkedAccountKeys> _query;
    protected QueryResult _result;
    protected IEnumerable<LinkedAccountKeys> Keys => ((IEnumerable<object>)_result.Data).Cast<LinkedAccountKeys>();
    protected virtual ClaimsPrincipal Principal => Session(_account);
    protected virtual bool Enabled => true;
    protected virtual bool Quarantine => false;

    async Task Establish()
    {
        await Propose(Quarantine);
        await Prepare();
        _query = new();
        var realmStore = Substitute.For<IEventStore>();
        realmStore.Namespace.Returns(EventStoreNamespaceName.Default);
        _query.Services.AddSingleton(realmStore);
        _query.Services.AddScoped<AccountReader>();
        var principal = Principal;
        _query.Services.AddSingleton<ICurrentPrincipalAccessor>(new FixedAccountPrincipal(principal));
        var context = Substitute.For<IHttpContextAccessor>();
        context.HttpContext.Returns(new DefaultHttpContext { User = principal });
        _query.Services.AddSingleton(context);
        _options.Accounts.EventSourced = Enabled;
        _query.Services.AddSingleton(Options.Create(_options));
        _query.Services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Development });
        _query.Given.ForEventSource(_account.Value).ReadModel(_states.InstanceForEventSourceId(_account.Value)!);
        _query.Given.ForEventSource(_account.Value).ReadModel(_profiles.InstanceForEventSourceId(_account.Value)!);
        var other = AccountSubject.New();
        await Seed(other, GitHubIssuer, "someone-else");
        await Refresh();
        _query.Given.ForEventSource(other.Value).ReadModel(_states.InstanceForEventSourceId(other.Value)!);
        _query.Given.ForEventSource(other.Value).ReadModel(_profiles.InstanceForEventSourceId(other.Value)!);
    }

    protected virtual Task Prepare() => Task.CompletedTask;

    protected async Task Perform() => _result = await _query.Perform(nameof(LinkedAccountKeys.ForCurrentAccount));

    void Destroy() => _query.Dispose();
}
