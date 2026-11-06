// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Constraints;

/// <summary>Reserves OAuth identities across active client streams.</summary>
public class UniqueClientId : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder.Unique(unique => unique
        .WithName("unique-client-id")
        .WithMessage("The client id is already registered.")
        .WithMode(UniqueConstraintMode.PerValue)
        .On<ClientRegistered>(@event => @event.ClientId)
        .RemovedWith<ClientRetired>());
}
