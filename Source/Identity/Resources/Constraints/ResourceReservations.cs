// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Resources.Events;

namespace Cratis.Identity.Resources.Constraints;

/// <summary>Permanently reserves resource identities and scopes, with no retirement removal.</summary>
internal sealed class ResourceReservations : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .Unique(unique => unique.WithName("unique-resource-indicator").WithMessage("The resource indicator is already reserved.")
            .WithMode(UniqueConstraintMode.PerValue).On<ResourceRegistrationRecorded>(@event => @event.ResourceIndicator))
        .Unique(unique => unique.WithName("unique-resource-audience").WithMessage("The resource audience is already reserved.")
            .WithMode(UniqueConstraintMode.PerValue).On<ResourceRegistrationRecorded>(@event => @event.Audience))
        .Unique(unique => unique.WithName("unique-resource-scope-prefix").WithMessage("The resource scope prefix is already reserved.")
            .WithMode(UniqueConstraintMode.PerValue).On<ResourceRegistrationRecorded>(@event => @event.ScopePrefix))
        .Unique(unique => unique.WithName("unique-resource-scope").WithMessage("The resource scope is already reserved.")
            .WithMode(UniqueConstraintMode.PerValue).On<ResourceScopeRegistrationRecorded>(@event => @event.ScopeName));
}
