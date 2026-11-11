// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Builds provider discovery from the configured public issuer, never from request headers.</summary>
internal static class OidcProviderMetadata
{
    /// <summary>Creates minimal key metadata when disabled, or the enabled code-only provider contract.</summary>
    /// <param name="options">The validated issuer configuration.</param>
    /// <param name="endpoints">The configured route prefix.</param>
    /// <returns>The public provider document.</returns>
    public static Dictionary<string, object> Create(IdentityOptions options, IdentityEndpointOptions endpoints)
    {
        var issuer = new Uri(options.Issuer, UriKind.Absolute);
        Dictionary<string, object> metadata = new(StringComparer.Ordinal)
        {
            ["issuer"] = options.Issuer,
            ["jwks_uri"] = new Uri(issuer, "/.well-known/jwks").AbsoluteUri
        };
        if (!options.Oidc.IssuanceEnabled) return metadata;
        AddProtocol(metadata, issuer, endpoints.RoutePrefix.Value ?? string.Empty);
        return metadata;
    }

    static void AddProtocol(Dictionary<string, object> metadata, Uri issuer, string prefix)
    {
        metadata["authorization_endpoint"] = Endpoint(issuer, prefix, "authorize");
        metadata["token_endpoint"] = Endpoint(issuer, prefix, "token");
        metadata["userinfo_endpoint"] = Endpoint(issuer, prefix, "userinfo");
        metadata["end_session_endpoint"] = Endpoint(issuer, prefix, "endsession");
        metadata["response_types_supported"] = new[] { ResponseTypes.Code };
        metadata["response_modes_supported"] = new[] { ResponseModes.Query, ResponseModes.FormPost };
        metadata["grant_types_supported"] = new[] { GrantTypes.AuthorizationCode, GrantTypes.RefreshToken };
        metadata["subject_types_supported"] = new[] { "public" };
        metadata["scopes_supported"] = new[] { Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess };
        metadata["id_token_signing_alg_values_supported"] = new[] { "RS256" };
        metadata["token_endpoint_auth_methods_supported"] = new[] { ClientAuthenticationMethods.ClientSecretBasic, ClientAuthenticationMethods.ClientSecretPost };
        metadata["code_challenge_methods_supported"] = new[] { CodeChallengeMethods.Sha256 };
        metadata["claims_supported"] = new[] { Claims.Issuer, Claims.Subject, Claims.Audience, Claims.Expiration, Claims.IssuedAt, Claims.AuthorizedParty, Claims.Nonce, Claims.AuthenticationTime, OidcIdentity.SessionIdClaim, Claims.AccessTokenHash, Claims.Name, Claims.PreferredUsername };
    }

    static string Endpoint(Uri issuer, string prefix, string name) => new Uri(issuer, $"{prefix}/connect/{name}").AbsoluteUri;
}
