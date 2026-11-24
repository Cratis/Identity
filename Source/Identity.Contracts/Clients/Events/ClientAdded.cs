// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>
/// A client was added to the realm. Published exactly once per client, on the stream identified by its <see cref="ClientRegistrationId"/>.
/// The redirect URIs follow as <see cref="RedirectUrisChangedForClient"/> and a confidential client's secret as
/// <see cref="SecretRotatedForClient"/>. It conveys no delegated authority. The business label stays Internal (amendment decision 4).
/// </summary>
/// <param name="ClientId">The protocol client id (an absolute CIMD URL or an opaque id). It is data, never the stream identity.</param>
/// <param name="RegistrationKind">The registration origin.</param>
/// <param name="RegisteredAt">The registration time.</param>
/// <param name="ClientType">The client type.</param>
/// <param name="ApplicationType">The application type.</param>
/// <param name="RequirePkce">Whether PKCE is required.</param>
/// <param name="ConsentType">The consent requirement.</param>
/// <param name="Endpoints">The explicit endpoint permissions.</param>
/// <param name="GrantTypes">The explicit grant permissions.</param>
/// <param name="ResponseTypes">The explicit response permissions.</param>
/// <param name="ResourceIndicators">The exact resource indicators.</param>
/// <param name="Scopes">The explicitly registered scope names.</param>
[EventType, Public]
public record ClientAdded(
    string ClientId,
    ClientRegistrationKind RegistrationKind,
    DateTimeOffset RegisteredAt,
    ClientType ClientType,
    ClientApplicationType ApplicationType,
    bool RequirePkce,
    ClientConsentType ConsentType,
    IEnumerable<ClientEndpoint> Endpoints,
    IEnumerable<ClientGrantType> GrantTypes,
    IEnumerable<ClientResponseType> ResponseTypes,
    IEnumerable<string> ResourceIndicators,
    IEnumerable<string> Scopes);
