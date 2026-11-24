// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Contracts.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using ClientKind = Cratis.Identity.Contracts.Clients.ClientType;

namespace Cratis.Identity.Clients.Serving;

/// <summary>Turns a registered client into the OpenIddict application the protocol endpoints serve.</summary>
internal static class RegisteredClientDescriptors
{
    /// <summary>Describes the client the way the protocol endpoints see it.</summary>
    /// <param name="registration">The client's registration.</param>
    /// <returns>The descriptor, without a secret: the secret is verified against the registration's digests.</returns>
    /// <exception cref="InvalidOperationException">The registration violates the client registration rules.</exception>
    internal static OpenIddictApplicationDescriptor For(ClientRegistration registration)
    {
        var configuration = registration.Configuration;
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = registration.ClientId?.Value ?? throw new InvalidOperationException("client_not_registered"),
            ClientType = configuration.ClientType == ClientKind.Public ? ClientTypes.Public : ClientTypes.Confidential,
            ApplicationType = configuration.ApplicationType == ClientApplicationType.Native ? ApplicationTypes.Native : ApplicationTypes.Web,
            ConsentType = configuration.ConsentType == ClientConsentType.Explicit ? ConsentTypes.Explicit : ConsentTypes.Implicit,
            DisplayName = string.IsNullOrWhiteSpace(configuration.DisplayName.Value) ? registration.ClientId.Value : configuration.DisplayName.Value
        };
        if (configuration.RequirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        foreach (var endpoint in configuration.Endpoints)
        {
            descriptor.Permissions.Add(Permission(endpoint));
        }

        foreach (var grant in configuration.GrantTypes)
        {
            descriptor.Permissions.Add(Permission(grant));
        }

        foreach (var response in configuration.ResponseTypes)
        {
            descriptor.Permissions.Add(response == ClientResponseType.Code ? Permissions.ResponseTypes.Code : throw new InvalidOperationException("invalid_client_configuration"));
        }

        foreach (var scope in configuration.Scopes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        foreach (var indicator in configuration.ResourceIndicators)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Resource + indicator);
        }

        foreach (var redirect in configuration.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirect, UriKind.Absolute));
        }

        var validation = ClientRegistrationRules.Validate(
            Confidential(descriptor),
            [CodeChallengeMethods.Sha256]);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Refusals));
        }

        return descriptor;
    }

    /// <summary>
    /// The rules insist a confidential client has a secret; the registered one is verified by digest, so a stand-in is used
    /// only to run the same rules.
    /// </summary>
    /// <param name="descriptor">The client's descriptor.</param>
    /// <returns>The descriptor to validate.</returns>
    static OpenIddictApplicationDescriptor Confidential(OpenIddictApplicationDescriptor descriptor)
    {
        if (descriptor.ClientType != ClientTypes.Confidential)
        {
            return descriptor;
        }

        var copy = new OpenIddictApplicationDescriptor
        {
            ClientId = descriptor.ClientId,
            ClientType = descriptor.ClientType,
            ApplicationType = descriptor.ApplicationType,
            ClientSecret = "digest-verified"
        };
        copy.Requirements.UnionWith(descriptor.Requirements);
        copy.RedirectUris.UnionWith(descriptor.RedirectUris);
        return copy;
    }

    static string Permission(ClientEndpoint endpoint) => endpoint switch
    {
        ClientEndpoint.Authorization => Permissions.Endpoints.Authorization,
        ClientEndpoint.Token => Permissions.Endpoints.Token,
        ClientEndpoint.Revocation => Permissions.Endpoints.Revocation,
        ClientEndpoint.Introspection => Permissions.Endpoints.Introspection,
        ClientEndpoint.DeviceAuthorization => Permissions.Endpoints.DeviceAuthorization,
        _ => throw new InvalidOperationException("invalid_client_configuration")
    };

    static string Permission(ClientGrantType grant) => grant switch
    {
        ClientGrantType.AuthorizationCode => Permissions.GrantTypes.AuthorizationCode,
        ClientGrantType.RefreshToken => Permissions.GrantTypes.RefreshToken,
        ClientGrantType.DeviceCode => Permissions.GrantTypes.DeviceCode,
        ClientGrantType.TokenExchange => Permissions.Prefixes.GrantType + Authorization.TokenExchange.TokenExchangeConstants.GrantType,
        _ => throw new InvalidOperationException("invalid_client_configuration")
    };
}
