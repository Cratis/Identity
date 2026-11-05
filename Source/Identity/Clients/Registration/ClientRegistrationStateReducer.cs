// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Registration;

/// <summary>Folds only internal client facts.</summary>
public class ClientRegistrationStateReducer : IReducerFor<ClientRegistrationState>
{
    /// <summary>Gets the reducer identity.</summary>
    public ReducerId Id => "6d0c2084-c36c-4a76-91ae-cbf475daeb11";

    /// <summary>Registers an OAuth identity.</summary>
    /// <param name="event">The registration fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The event source.</param>
    /// <returns>The next state.</returns>
    public ClientRegistrationState On(ClientRegistered @event, ClientRegistrationState? current, EventContext context) => Start(current, context) with
    {
        ClientId = @event.ClientId,
        RegistrationKind = @event.RegistrationKind
    };

    /// <summary>Replaces the accepted redirect set.</summary>
    /// <param name="event">The redirect fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The event source.</param>
    /// <returns>The next state.</returns>
    public ClientRegistrationState On(ClientRedirectUrisChanged @event, ClientRegistrationState? current, EventContext context) => Start(current, context) with { RedirectUris = @event.RedirectUris };

    /// <summary>Replaces the current secret comparand without an overlap window.</summary>
    /// <param name="event">The rotation fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The event source.</param>
    /// <returns>The next state.</returns>
    public ClientRegistrationState On(ClientSecretRotated @event, ClientRegistrationState? current, EventContext context) => Start(current, context) with
    {
        CurrentSecretId = @event.ClientSecretId,
        CurrentSecretHash = @event.ClientSecretHash,
        SecretActivatedAt = @event.ActivatedAt
    };

    /// <summary>Retires a registration.</summary>
    /// <param name="event">The retirement fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The event source.</param>
    /// <returns>The next state.</returns>
    public ClientRegistrationState On(ClientRetired @event, ClientRegistrationState? current, EventContext context)
    {
        _ = @event;
        return Start(current, context) with { Retired = true };
    }

    static ClientRegistrationState Start(ClientRegistrationState? current, EventContext context) => current ?? new(context.EventSourceId.Value);
}
