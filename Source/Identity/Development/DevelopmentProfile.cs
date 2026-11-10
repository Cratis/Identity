// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.Development;

/// <summary>
/// The opt-in local-development profile: a loopback issuer, its own persisted keys, local product resources and
/// pre-registered public clients, so a local tool can sign in against a local Identity and call a local resource server.
/// </summary>
/// <remarks>
/// The profile is enabled only by <c language="text">Identity:Development:Enabled</c>. Outside the Development environment
/// that setting stops the host at startup. The profile never merges with deployed configuration: it refuses a configured
/// secret store, replaces the resources, and keeps its keys in a dedicated per-user store no deployed host reads.
/// </remarks>
internal static class DevelopmentProfile
{
    /// <summary>
    /// The audience of the Direct MCP resource the profile registers. The one place to change it: the profile's settings,
    /// the documentation and the specs all name this value.
    /// </summary>
    internal const string DirectMcpAudience = "direct-mcp";

    /// <summary>
    /// The audience of the Direct REST API, which the profile does not register; a token for the MCP audience is never valid on it.
    /// </summary>
    internal const string DirectApiAudience = "direct-api";

    static readonly string[] _loopbackHosts = ["localhost", "127.0.0.1", "[::1]"];

    /// <summary>
    /// Applies the profile when it is enabled.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
    /// <param name="startupOptions">The startup <see cref="IdentityOptions"/>, changed in place when the profile is active.</param>
    /// <returns>The applied profile when it is active; otherwise <see langword="null"/>.</returns>
    /// <exception cref="DevelopmentProfileOnlyInDevelopment">Thrown when the profile is enabled outside Development.</exception>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the configuration or the key store is not safe.</exception>
    public static AppliedDevelopmentProfile? Apply(IConfiguration configuration, IHostEnvironment environment, IdentityOptions startupOptions) =>
        Apply(configuration, environment, startupOptions, new DevelopmentKeyStore(DevelopmentKeyStore.DefaultRoot, environment.ContentRootPath, TimeProvider.System), () => EnsureParent(DevelopmentKeyStore.DefaultRoot));

    /// <summary>
    /// Applies the profile when it is enabled, with an explicit key store.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
    /// <param name="startupOptions">The startup <see cref="IdentityOptions"/>, changed in place when the profile is active.</param>
    /// <param name="store">The <see cref="DevelopmentKeyStore"/>.</param>
    /// <param name="prepareParent">Optional host bootstrap of the per-user app-data parent, outside key custody.</param>
    /// <returns>The applied profile when it is active; otherwise <see langword="null"/>.</returns>
    /// <exception cref="DevelopmentProfileOnlyInDevelopment">Thrown when the profile is enabled outside Development.</exception>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the configuration or the key store is not safe.</exception>
    internal static AppliedDevelopmentProfile? Apply(IConfiguration configuration, IHostEnvironment environment, IdentityOptions startupOptions, DevelopmentKeyStore store, Action? prepareParent = null)
    {
        var development = configuration.GetSection($"{IdentityOptions.SectionName}:{DevelopmentOptions.SectionName}").Get<DevelopmentOptions>();
        if (development?.Enabled != true)
        {
            return null;
        }

        if (!environment.IsDevelopment())
        {
            throw new DevelopmentProfileOnlyInDevelopment(environment.EnvironmentName);
        }

        var issuer = Validate(configuration, startupOptions, development);
        store.ValidateLocation(Authority(issuer));
        prepareParent?.Invoke();
        var directory = store.Provision(Authority(issuer));

        startupOptions.SecretStore.Path = directory;
        startupOptions.Resources = [.. development.Resources];

        return new AppliedDevelopmentProfile(development, store.Custody);
    }

    internal static void EnsureParent(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
        {
            throw new DevelopmentProfileRefused("the per-user application data folder is not available on this machine.");
        }

        // Host bootstrap creates only application-data folders, never a key-store leaf or key material.
        // The store subsequently validates every ancestor and pins its leaf before touching any secrets.
        var parent = Path.Combine(dataRoot, "cratis", "identity-dev");
        if (Directory.Exists(parent))
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            WindowsOwnerOnlyAccess.CreateDirectory(parent);
        }
        else
        {
            Directory.CreateDirectory(parent, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    static string Authority(Uri issuer) => new([.. issuer.Authority.ToLowerInvariant().Select(_ => char.IsAsciiLetterOrDigit(_) ? _ : '-')]);

    static bool IsLoopbackOrigin(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp && _loopbackHosts.Contains(uri.Host, StringComparer.Ordinal) && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);

    static Uri Validate(IConfiguration configuration, IdentityOptions startupOptions, DevelopmentOptions development)
    {
        if (!Uri.TryCreate(startupOptions.Issuer, UriKind.Absolute, out var issuer) || !IsLoopbackOrigin(issuer))
        {
            throw new DevelopmentProfileRefused("Identity:Issuer must be an http loopback origin (localhost, 127.0.0.1 or [::1]); a deployed issuer is never used locally.");
        }

        if (startupOptions.SecretStore.IsConfigured)
        {
            throw new DevelopmentProfileRefused("Identity:SecretStore:Path is configured. The profile owns key custody and never reads a secret store; remove the setting.");
        }

        if (configuration["Identity:Hosting:Preset"] == "hosted-shared")
        {
            throw new DevelopmentProfileRefused("Identity:Hosting:Preset is hosted-shared. The profile is not a hosted configuration.");
        }

        if (development.Resources.Count == 0)
        {
            throw new DevelopmentProfileRefused("Identity:Development:Resources must list at least one local resource.");
        }

        if (development.Resources.SelectMany(_ => _.Indicators).Any(_ => !Uri.TryCreate(_, UriKind.Absolute, out var uri) || !IsLoopbackOrigin(uri)))
        {
            throw new DevelopmentProfileRefused("Every Identity:Development:Resources indicator must be an http loopback URI; a deployed resource indicator is never used locally.");
        }

        ValidateClients(development.Clients);
        return issuer;
    }

    static bool HasDuplicateIds(IList<DevelopmentClientOptions> clients) =>
        clients.Select(_ => _.ClientId).Distinct(StringComparer.Ordinal).Count() != clients.Count;

    static bool IsIncomplete(DevelopmentClientOptions client) => string.IsNullOrWhiteSpace(client.ClientId) || client.RedirectUris.Count == 0;

    static void ValidateClients(IList<DevelopmentClientOptions> clients)
    {
        if (clients.Any(IsIncomplete) || HasDuplicateIds(clients))
        {
            throw new DevelopmentProfileRefused("Every Identity:Development:Clients entry needs a unique ClientId and at least one redirect URI.");
        }

        if (clients.Any(_ => string.Equals(_.ClientId, Clients.ClientDescriptors.CratisCli, StringComparison.Ordinal)))
        {
            throw new DevelopmentProfileRefused("A development client cannot reuse the built-in client id.");
        }

        if (clients.SelectMany(_ => _.RedirectUris).Any(_ => !Uri.TryCreate(_, UriKind.Absolute, out var uri) || !IsLoopbackOrigin(uri)))
        {
            throw new DevelopmentProfileRefused("Every development client redirect URI must be an exact http loopback URI.");
        }
    }
}
