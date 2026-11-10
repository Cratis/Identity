// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Contracts.Resources.Events;

namespace Cratis.Identity.Resources;

/// <summary>Folds only Internal resource facts, preserving permanent scope tombstones.</summary>
internal sealed class ResourceRegistrationReducer : IReducerFor<ResourceRegistration>
{
    /// <summary>Gets the stable Internal reducer identity.</summary>
    public ReducerId Id => "99765a0a-f9a6-4c6a-839e-606aa84370d7";

    /// <summary>Records the resource's immutable catalogue identity and Internal label.</summary>
    /// <param name="event">The Internal source fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The resource stream.</param>
    /// <returns>The next state.</returns>
    public ResourceRegistration On(ResourceRegistrationRecorded @event, ResourceRegistration? current, EventContext context) => Start(current, context) with
    {
        ResourceIndicator = @event.ResourceIndicator,
        Audience = @event.Audience,
        ScopePrefix = @event.ScopePrefix,
        DisplayName = @event.DisplayName
    };

    /// <summary>Adds a scope without overwriting an existing reservation or retirement.</summary>
    /// <param name="event">The Internal source fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The resource stream.</param>
    /// <returns>The next state.</returns>
    public ResourceRegistration On(ResourceScopeRegistrationRecorded @event, ResourceRegistration? current, EventContext context)
    {
        var state = Start(current, context);
        return state.Scopes.Any(scope => scope.ScopeName == @event.ScopeName) ? state : state with
        {
            Scopes = [.. state.Scopes, new(@event.ScopeName, @event.Description)]
        };
    }

    /// <summary>Retains a permanent tombstone, even when replay encounters a missing registration.</summary>
    /// <param name="event">The Internal source fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The resource stream.</param>
    /// <returns>The next state.</returns>
    public ResourceRegistration On(ResourceScopeRetirementRecorded @event, ResourceRegistration? current, EventContext context)
    {
        var state = Start(current, context);
        var scopes = state.Scopes.Any(scope => scope.ScopeName == @event.ScopeName) ? state.Scopes :
            [.. state.Scopes, new ResourceScopeRegistration(@event.ScopeName, new(string.Empty))];
        return state with
        {
            Scopes = scopes.Select(scope => scope.ScopeName == @event.ScopeName && !scope.Retired ?
                scope with { Retired = true, RetiredAt = @event.RetiredAt } : scope).ToArray()
        };
    }

    static ResourceRegistration Start(ResourceRegistration? current, EventContext context) => current ?? new(new(Guid.Parse(context.EventSourceId)));
}
