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
}
