// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// An <see cref="IdentityHost"/> with the local-development profile switched on, as <c language="text">Identity__Development__Enabled</c>
/// does for a developer: the shipped <c language="text">appsettings.Development.json</c> supplies the loopback resources and clients, and
/// the profile provisions its own keys in the per-user development key store.
/// </summary>
public class DevelopmentProfileHost : IdentityHost
{
    public const string LocalIssuer = "http://127.0.0.1:5100/";
    public const string LocalClient = "local-claude";
    public const string LocalRedirect = "http://localhost:53123/callback";
    public const string LocalDirectMcp = "http://localhost:5200/mcp";
    public const string LocalTenant = "local";
    public const string DirectMcpAudience = DevelopmentProfile.DirectMcpAudience;
    public const string DirectApiAudience = DevelopmentProfile.DirectApiAudience;

    const string DataHomeVariable = "XDG_DATA_HOME";

    readonly string? _previousDataHome = Environment.GetEnvironmentVariable(DataHomeVariable);
    readonly string? _dataHome;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentProfileHost"/> class. On Unix the per-user data root moves to a
    /// fresh owner-only directory under the system temp root, so the profile's ancestor safety check is not decided by the group
    /// permissions of whichever home directory the machine running the specs has.
    /// </summary>
    public DevelopmentProfileHost()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        _dataHome = Path.Combine("/tmp", $"identity-dev-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataHome, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Environment.SetEnvironmentVariable(DataHomeVariable, _dataHome);
    }

    /// <summary>
    /// Creates a browser whose base address is the loopback IP issuer, not <c language="text">localhost</c>, so a redirect to a client's
    /// <c language="text">localhost</c> callback stops there instead of being followed back into the host.
    /// </summary>
    /// <returns>The <see cref="Browser"/>.</returns>
    public Browser CreateLocalBrowser()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri(LocalIssuer)
        });
        client.DefaultRequestHeaders.Add(FakeUpstream.UserHeader, FakeUpstream.DefaultUser);
        return new Browser(client);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_dataHome is null)
        {
            return;
        }

        Environment.SetEnvironmentVariable(DataHomeVariable, _previousDataHome);
        Directory.Delete(_dataHome, true);
    }

    /// <inheritdoc/>
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:Issuer"] = LocalIssuer;
        settings["Identity:Development:Enabled"] = "true";
        return settings;
    }
}
