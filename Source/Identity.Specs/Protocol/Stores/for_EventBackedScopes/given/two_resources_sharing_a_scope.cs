// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources;

namespace Cratis.Identity.Protocol.Stores.for_EventBackedScopes.given;

public class two_resources_sharing_a_scope : resource_facts
{
    private protected ResourceRegistration OtherResource;

    void Establish()
    {
        Resource = RegisterScope(Resource);
        var context = EventContext.EmptyWithEventSourceId(ResourceId.New().Value);
        var registered = Reducer.On(new ResourceRegistrationRecorded("https://reports.example", "reports", "api", new("Reports"), FactId.New()), null, context);
        OtherResource = Reducer.On(new ResourceScopeRegistrationRecorded("api:read", new("Read reports"), FactId.New()), registered, context);
    }
}
