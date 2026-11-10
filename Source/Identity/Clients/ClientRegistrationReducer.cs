// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

/// <summary>Folds Internal client facts without touching protocol stores or secret producers.</summary>
internal sealed class ClientRegistrationReducer : IReducerFor<ClientRegistration>
{
    /// <summary>Gets the stable Internal client reducer identity.</summary>
    public ReducerId Id => "de0acd1f-59ac-48db-bbe8-668545c5a32e";

    /// <summary>Records accepted configuration.</summary>
    /// <param name="event">The Internal fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The source stream.</param>
    /// <returns>The next state.</returns>
    public ClientRegistration On(ClientRegistered @event, ClientRegistration? current, EventContext context) => Start(current, context) with
    {
        ClientId = new(@event.ClientId),
        Configuration = new()
        {
            ClientType = @event.ClientType,
            ApplicationType = @event.ApplicationType,
            DisplayName = @event.DisplayName,
            RequirePkce = @event.RequirePkce,
            ConsentType = @event.ConsentType,
            Endpoints = @event.Endpoints.ToArray(),
            GrantTypes = @event.GrantTypes.ToArray(),
            ResponseTypes = @event.ResponseTypes.ToArray(),
            ResourceIndicators = @event.ResourceIndicators.ToArray(),
            Scopes = @event.Scopes.ToArray()
        }
    };

    /// <summary>Replaces the exact redirect allowlist.</summary>
    /// <param name="event">The Internal fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The source stream.</param>
    /// <returns>The next state.</returns>
    public ClientRegistration On(ClientRedirectUrisChanged @event, ClientRegistration? current, EventContext context)
    {
        var state = Start(current, context);
        return state with { Configuration = state.Configuration with { RedirectUris = @event.RedirectUris.ToArray() } };
    }

    /// <summary>Changes digest revisions without recovering a credential.</summary>
    /// <param name="event">The Internal fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The source stream.</param>
    /// <returns>The next state.</returns>
    public ClientRegistration On(ClientSecretRotated @event, ClientRegistration? current, EventContext context)
    {
        var state = Start(current, context);
        if (state.Retired)
        {
            return state;
        }

        var previous = state.PreviousSecretRevisions.AsEnumerable();
        if (state.SecretHash is not null)
        {
            previous = previous.Append(new(state.SecretHash, state.ActivatedAt, @event.PreviousValidUntil));
        }

        return state with
        {
            PreviousSecretHash = state.SecretHash,
            PreviousSecretRevisions = previous.ToArray(),
            SecretId = @event.ClientSecretId,
            SecretHash = @event.ClientSecretHash,
            ActivatedAt = @event.ActivatedAt,
            PreviousValidUntil = @event.PreviousValidUntil
        };
    }

    /// <summary>Permanently retires the client while retaining its identifier reservation.</summary>
    /// <param name="event">The Internal fact.</param>
    /// <param name="current">The previous state.</param>
    /// <param name="context">The source stream.</param>
    /// <returns>The next state.</returns>
    public ClientRegistration On(ClientRetired @event, ClientRegistration? current, EventContext context) => Start(current, context) with
    {
        Retired = true,
        RetiredAt = @event.RetiredAt,
        SecretId = null,
        SecretHash = null,
        PreviousSecretHash = null,
        PreviousSecretRevisions = []
    };

    static ClientRegistration Start(ClientRegistration? current, EventContext context) => current ?? new(Guid.Parse(context.EventSourceId));
}
