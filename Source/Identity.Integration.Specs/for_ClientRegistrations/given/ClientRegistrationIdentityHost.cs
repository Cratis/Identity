// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_ClientRegistrations.given;

/// <summary>The production host, with only upstream authentication replaced by the existing fake.</summary>
/// <param name="settings">The real Mongo and Chronicle endpoints and client policy.</param>
public class ClientRegistrationIdentityHost(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    /// <summary>Creates a cookie-preserving browser through the production endpoints.</summary>
    /// <returns>The browser.</returns>
    public Browser CreateBrowser()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new(IdentityHost.Issuer)
        });
        client.DefaultRequestHeaders.Add(FakeUpstream.UserHeader, FakeUpstream.DefaultUser);
        return new(client);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in settings.Where(pair => pair.Value is not null))
        {
            builder.UseSetting(key, value);
        }

        builder.UseEnvironment("Development")
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings))
            .ConfigureTestServices(services =>
            {
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, FakeUpstream>(FakeUpstream.Scheme, _ => { });
                services.Configure<UpstreamLoginOptions>(options => options.Scheme = FakeUpstream.Scheme);
            });
    }
}
