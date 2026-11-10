// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal client registration configuration; it conveys no delegated authority.</summary>
/// <param name="ClientId">The absolute CIMD URL or opaque protocol id, never the stream identity.</param>
/// <param name="RegistrationKind">The registration origin.</param>
/// <param name="RegisteredAt">The registration time.</param>
/// <param name="ClientType">The client type.</param>
/// <param name="ApplicationType">The application type.</param>
/// <param name="DisplayName">The non-person application label, Internal only.</param>
/// <param name="RequirePkce">Whether PKCE is required.</param>
/// <param name="ConsentType">The consent requirement.</param>
/// <param name="Endpoints">The explicit endpoint permissions.</param>
/// <param name="GrantTypes">The explicit grant permissions.</param>
/// <param name="ResponseTypes">The explicit response permissions.</param>
/// <param name="ResourceIndicators">The exact resource indicators.</param>
/// <param name="Scopes">The explicitly registered scope names.</param>
[EventType]
public record ClientRegistered(
    string ClientId,
    ClientRegistrationKind RegistrationKind,
    DateTimeOffset RegisteredAt,
    ClientType ClientType,
    ClientApplicationType ApplicationType,
    ClientDisplayName DisplayName,
    bool RequirePkce,
    ClientConsentType ConsentType,
    IEnumerable<ClientEndpoint> Endpoints,
    IEnumerable<ClientGrantType> GrantTypes,
    IEnumerable<ClientResponseType> ResponseTypes,
    IEnumerable<string> ResourceIndicators,
    IEnumerable<string> Scopes);
