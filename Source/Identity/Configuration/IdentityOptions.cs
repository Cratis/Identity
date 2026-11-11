// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents the configuration of Cratis Identity, bound from the <c language="text">Identity</c> configuration section.
/// </summary>
public class IdentityOptions
{
    /// <summary>
    /// The name of the configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Identity";

    /// <summary>
    /// Gets or sets the absolute issuer URI tokens are issued under, for example <c language="text">https://auth.cratis.studio/</c>.
    /// </summary>
    /// <remarks>
    /// Required outside development. Resource servers validate the <c language="text">iss</c> claim against this exact value.
    /// </remarks>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subjects explicitly authorized to administer issuer client and resource registrations.
    /// An empty list grants no administrative authority.
    /// </summary>
    public IList<string> SecurityOwners { get; set; } = [];

    /// <summary>Gets or sets public Client ID Metadata Document policy.</summary>
    public IdentityOptionsClients Clients { get; set; } = new();

    /// <summary>Gets or sets the native account options.</summary>
    public AccountsOptions Accounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the MongoDB storage configuration.
    /// </summary>
    public MongoDBOptions MongoDB { get; set; } = new();

    /// <summary>
    /// Gets or sets the GitHub upstream login configuration.
    /// </summary>
    public GitHubOptions GitHub { get; set; } = new();

    /// <summary>
    /// Gets or sets the secret store the realm signing and encryption keys and the Data Protection key ring are read from.
    /// </summary>
    public SecretStoreOptions SecretStore { get; set; } = new();

    /// <summary>
    /// Gets or sets token lifetimes.
    /// </summary>
    public TokenOptions Tokens { get; set; } = new();

    /// <summary>Gets or sets device authorization limits.</summary>
    [ConfigurationKeyName("Device")]
    public DeviceGrantOptions DeviceGrant { get; set; } = new();

    /// <summary>Gets or sets token-endpoint DPoP sender binding.</summary>
    public DPoPOptions DPoP { get; set; } = new();

    /// <summary>Gets or sets the end-session hint limits.</summary>
    public EndSessionOptions Sessions { get; set; } = new();

    /// <summary>
    /// Gets or sets the lifetime of the Identity site session cookie.
    /// </summary>
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the resource servers tokens can be issued for.
    /// </summary>
    public IList<ResourceServerOptions> Resources { get; set; } = [];

    /// <summary>
    /// Gets or sets the confidential clients resource servers use to call the introspection endpoint.
    /// </summary>
    public IList<IntrospectionClientOptions> IntrospectionClients { get; set; } = [];

    /// <summary>
    /// Gets or sets the confidential OpenID Connect clients. Empty by default, leaving OIDC clients disabled.
    /// </summary>
    public IList<OidcClientOptions> OidcClients { get; set; } = [];

    /// <summary>
    /// Gets or sets the OpenID Connect identity-only issuance gate.
    /// </summary>
    public OidcOptions Oidc { get; set; } = new();

    /// <summary>
    /// Gets or sets the RFC 8693 token-exchange configuration. Empty by default, which leaves the grant disabled.
    /// </summary>
    public TokenExchangeOptions TokenExchange { get; set; } = new();

    /// <summary>
    /// Gets or sets the per-process token endpoint admission limits.
    /// </summary>
    public TokenRateLimitOptions TokenRateLimit { get; set; } = new();
}
