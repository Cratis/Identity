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
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:Issuer"] = LocalIssuer;
        settings["Identity:Development:Enabled"] = "true";
        return settings;
    }
}
