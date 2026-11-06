// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Registration;

/// <summary>Admits guarded reads of the complete client stream.</summary>
public class ClientGuardProjection : IProjectionFor<ClientGuard>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ClientGuard> builder) => builder
        .From<ClientRegistered>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<ClientRedirectUrisChanged>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<ClientSecretRotated>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<ClientRetired>(from => from.Set(model => model.Id).ToEventSourceId());
}
