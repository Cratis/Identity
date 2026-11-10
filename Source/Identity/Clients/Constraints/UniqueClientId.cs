// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Constraints;

/// <summary>Permanently reserves each exact protocol client identifier, including after retirement.</summary>
internal sealed class UniqueClientId : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder.Unique(unique => unique
        .WithName("unique-client-id")
        .WithMessage("The client id is already reserved.")
        .WithMode(UniqueConstraintMode.PerValue)
        .On<ClientRegistered>(@event => @event.ClientId));
}
