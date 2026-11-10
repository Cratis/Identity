// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using ClientKind = Cratis.Identity.Contracts.Clients.ClientType;

namespace Cratis.Identity.Clients;

/// <summary>Accepted, non-secret client configuration. No request-time credentials belong here.</summary>
internal sealed record ClientConfiguration
{
    /// <summary>Gets the client authentication type.</summary>
    public ClientKind ClientType { get; init; } = ClientKind.Public;

    /// <summary>Gets the application type.</summary>
    public ClientApplicationType ApplicationType { get; init; } = ClientApplicationType.Web;

    /// <summary>Gets the display name.</summary>
    public ClientDisplayName DisplayName { get; init; } = new(string.Empty);

    /// <summary>Gets whether PKCE is required.</summary>
    public bool RequirePkce { get; init; } = true;

    /// <summary>Gets the consent policy.</summary>
    public ClientConsentType ConsentType { get; init; } = ClientConsentType.Explicit;

    /// <summary>Gets the endpoint permissions.</summary>
    public ClientEndpoint[] Endpoints { get; init; } = [];

    /// <summary>Gets the grant types.</summary>
    public ClientGrantType[] GrantTypes { get; init; } = [];

    /// <summary>Gets the response types.</summary>
    public ClientResponseType[] ResponseTypes { get; init; } = [];

    /// <summary>Gets the allowed resources.</summary>
    public string[] ResourceIndicators { get; init; } = [];

    /// <summary>Gets the scope ceiling.</summary>
    public string[] Scopes { get; init; } = [];

    /// <summary>Gets the exact redirect allowlist.</summary>
    public string[] RedirectUris { get; init; } = [];

    /// <summary>Delegates registration validation to the existing rules.</summary>
    /// <param name="secret">The command secret, or a presence marker when validating existing configuration.</param>
    /// <returns>A credential-free refusal, or null.</returns>
    public ValidationResult? Validate(string? secret)
    {
        if (!HasSupportedKinds() || !HasSupportedPermissions())
        {
            return ValidationResult.Error("invalid_client_configuration");
        }

        if (ClientType == ClientKind.Public && secret is not null)
        {
            return ValidationResult.Error("invalid_client_configuration");
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientType = ClientType == ClientKind.Public ? ClientTypes.Public : ClientTypes.Confidential,
            ApplicationType = ApplicationType == ClientApplicationType.Native ? ApplicationTypes.Native : ApplicationTypes.Web,
            ClientSecret = secret
        };
        if (RequirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        foreach (var value in RedirectUris)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return ValidationResult.Error("invalid_client_redirect_uri");
            }

            descriptor.RedirectUris.Add(uri);
        }

        var validation = ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]);
        return validation.IsValid ? null : ValidationResult.Error("invalid_client_configuration");
    }

    /// <summary>Creates the Internal registration fact; redirects and initial secret have their own facts.</summary>
    /// <param name="clientId">The validated protocol identifier.</param>
    /// <param name="at">The trusted registration time.</param>
    /// <returns>The non-secret configuration fact.</returns>
    public ClientRegistered Registered(ClientId clientId, DateTimeOffset at) => new(
        clientId.Value,
        ClientRegistrationKind.PreRegistered,
        at,
        ClientType,
        ApplicationType,
        DisplayName,
        RequirePkce,
        ConsentType,
        Endpoints.ToArray(),
        GrantTypes.ToArray(),
        ResponseTypes.ToArray(),
        ResourceIndicators.ToArray(),
        Scopes.ToArray());

    bool HasSupportedKinds() => Enum.IsDefined(ClientType) && Enum.IsDefined(ApplicationType) && Enum.IsDefined(ConsentType);

    bool HasSupportedPermissions() => Endpoints.All(Enum.IsDefined) && GrantTypes.All(Enum.IsDefined) && ResponseTypes.All(Enum.IsDefined);
}
