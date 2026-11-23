// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources;

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes.given;

public class resource_facts : Specification
{
    private protected ResourceRegistrationReducer Reducer;
    private protected EventContext Context;
    private protected ResourceRegistration Resource;

    void Establish()
    {
        Reducer = new();
        Context = EventContext.EmptyWithEventSourceId(ResourceId.New().Value);
        Resource = Reducer.On(Registered(), null, Context);
    }

    private protected static ResourceRegistrationRecorded Registered() =>
        new("https://api.example", "api", "api", new("API"), FactId.New());

    private protected ResourceRegistration RegisterScope(ResourceRegistration? current, string name = "api:read", string description = "Read API") =>
        Reducer.On(new ResourceScopeRegistrationRecorded(name, new(description), FactId.New()), current, Context);

    private protected ResourceRegistration RetireScope(ResourceRegistration? current) =>
        Reducer.On(new ResourceScopeRetirementRecorded("api:read", DateTimeOffset.UnixEpoch.AddDays(10), FactId.New()), current, Context);

    private protected static ConfigurationScopeStore StoreFor(params ResourceRegistration[] resources)
    {
        var catalog = Substitute.For<IProtocolCatalog>();
        catalog.Scopes.Returns(EventBackedScopes.From(resources));
        return new(catalog);
    }

    private protected static async Task<string[]> Names(IAsyncEnumerable<ResourceScope> scopes)
    {
        List<string> names = [];
        await foreach (var scope in scopes.WithCancellation(TestContext.Current.CancellationToken)) names.Add(scope.Name);
        return names.ToArray();
    }
}
