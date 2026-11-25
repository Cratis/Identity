// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Requests;

/// <summary>
/// Decides what Cratis Identity does about a request a product made on a person's behalf. A request is honored only for a security
/// owner, and only within what the registration commands already allow: a public native client with PKCE, or retiring a client that
/// was registered at runtime.
/// </summary>
internal static class ClientRequestDecisions
{
    /// <summary>Decides a request to register a public client.</summary>
    /// <param name="request">The request.</param>
    /// <param name="securityOwners">The subjects Cratis Identity treats as security owners.</param>
    /// <param name="reservedClientIds">The client ids the configuration declares.</param>
    /// <param name="servedScopes">The scope names Cratis Identity serves.</param>
    /// <param name="registrationId">The stream identity to give a new registration.</param>
    /// <param name="at">The server time.</param>
    /// <returns>The facts to append, or the reason for refusing.</returns>
    public static ClientRequestOutcome ForRegistration(
        ClientRegistrationRequested request,
        IReadOnlyCollection<string> securityOwners,
        IReadOnlySet<string> reservedClientIds,
        IReadOnlySet<string> servedScopes,
        ClientRegistrationId registrationId,
        DateTimeOffset at)
    {
        if (!IsSecurityOwner(request.RequestedBy, securityOwners))
        {
            return ClientRequestOutcome.Refuse("requester_not_security_owner");
        }

        var clientId = new ClientId(request.ClientId);
        if (!RegisterClient.IsValidClientId(clientId))
        {
            return ClientRequestOutcome.Refuse("invalid_client_registration");
        }

        if (reservedClientIds.Contains(request.ClientId))
        {
            return ClientRequestOutcome.Refuse("client_id_already_reserved");
        }

        var scopes = request.Scopes.Distinct(StringComparer.Ordinal).ToArray();
        if (scopes.Any(scope => !servedScopes.Contains(scope)))
        {
            return ClientRequestOutcome.Refuse("unknown_scope");
        }

        var configuration = new ClientConfiguration
        {
            ClientType = ClientType.Public,
            ApplicationType = ClientApplicationType.Native,
            DisplayName = new(request.ClientId),
            RequirePkce = true,
            ConsentType = ClientConsentType.Explicit,
            Endpoints = [ClientEndpoint.Authorization, ClientEndpoint.Token, ClientEndpoint.Revocation],
            GrantTypes = [ClientGrantType.AuthorizationCode, ClientGrantType.RefreshToken],
            ResponseTypes = [ClientResponseType.Code],
            Scopes = scopes,
            RedirectUris = [.. request.RedirectUris]
        };

        if (configuration.Validate(null) is not null)
        {
            return ClientRequestOutcome.Refuse("invalid_client_configuration");
        }

        return ClientRequestOutcome.Accept(
        [
            new(registrationId.Value.ToString(), configuration.Registered(clientId, at)),
            new(registrationId.Value.ToString(), new ClientRedirectUrisChanged(configuration.RedirectUris))
        ]);
    }

    /// <summary>Decides a request to retire a client.</summary>
    /// <param name="request">The request.</param>
    /// <param name="securityOwners">The subjects Cratis Identity treats as security owners.</param>
    /// <param name="registration">The registration the request names, or <see langword="null"/> when there is none.</param>
    /// <param name="at">The server time.</param>
    /// <returns>The facts to append, or the reason for refusing.</returns>
    public static ClientRequestOutcome ForRetirement(
        ClientRetirementRequested request,
        IReadOnlyCollection<string> securityOwners,
        ClientRegistration? registration,
        DateTimeOffset at)
    {
        if (!IsSecurityOwner(request.RequestedBy, securityOwners))
        {
            return ClientRequestOutcome.Refuse("requester_not_security_owner");
        }

        if (ClientRegistration.RequireActive(registration) is not null)
        {
            return ClientRequestOutcome.Refuse("client_not_active");
        }

        return ClientRequestOutcome.Accept([new(request.RegistrationId.Value.ToString(), new ClientRetired("owner_retired", at))]);
    }

    static bool IsSecurityOwner(string? subject, IReadOnlyCollection<string> securityOwners) =>
        !string.IsNullOrWhiteSpace(subject) && securityOwners.Contains(subject, StringComparer.Ordinal);
}
