// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle;
using Cratis.Identity.Accounts.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_querying_through_arc : given.a_consent_interaction
{
    QueryResult _owner;
    QueryResult _anonymous;

    async Task Because()
    {
        _owner = await Perform();
        context.HttpContext.User = new(new System.Security.Claims.ClaimsIdentity());
        _anonymous = await Perform();
    }

    async Task<QueryResult> Perform()
    {
        await using var scenario = new QueryScenario<PendingConsent>();
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Namespace.Returns(EventStoreNamespaceName.Default);
        scenario.Services.AddSingleton(eventStore);
        scenario.Services.AddSingleton(interaction);
        scenario.Services.AddSingleton(context);
        scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(new FixedAccountPrincipal(context.HttpContext.User));
        return await scenario.Perform(nameof(PendingConsent.ForCurrentSession), new QueryArguments { ["accountId"] = "00000000-0000-4000-8000-000000000099", ["sessionId"] = "other-session" });
    }

    [Fact] void should_authorize_the_owner() => _owner.IsAuthorized.ShouldBeTrue();
    [Fact] void should_ignore_submitted_targets() => ((PendingConsent)_owner.Data).Id.ShouldEqual(request.Id);
    [Fact] void should_deny_anonymous_queries() => _anonymous.IsAuthorized.ShouldBeFalse();
}
