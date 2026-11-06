// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Registration;

/// <summary>Indexes active registrations and removes retired identities.</summary>
public class ClientIdLookupProjection : IProjectionFor<ClientIdLookup>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ClientIdLookup> builder) => builder
        .From<ClientRegistered>(from => from.Set(model => model.Id).ToEventSourceId())
        .RemovedWith<ClientRetired>();
}
