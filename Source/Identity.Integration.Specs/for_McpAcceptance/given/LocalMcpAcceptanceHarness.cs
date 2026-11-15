// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Adapts the opt-in development profile; it never starts or changes a deployed realm.
/// </summary>
/// <param name="host">The existing local Identity fixture.</param>
public class LocalMcpAcceptanceHarness(DevelopmentProfileHost host) : IMcpAcceptanceHarness
{
    public Uri Issuer => new(host.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Issuer);
    public string ClientId => DevelopmentProfileHost.LocalClient;
    public string RedirectUri => DevelopmentProfileHost.LocalRedirect;
    public string Tenant => DevelopmentProfileHost.LocalTenant;
    public McpProduct Direct => Product("direct-mcp", "direct:mcp:read");
    public McpProduct Studio => Product("studio-mcp", "studio:mcp:read");

    public Browser CreateBrowser() => host.CreateLocalBrowser();

    public void PreventFurtherSignIns(Browser browser) => browser.Client.DefaultRequestHeaders.Remove(FakeUpstream.UserHeader);

    public HttpClient CreateIntrospectionClient(McpProduct product, DelegatingHandler handler)
    {
        var (id, secret) = product.Audience == ResourceSettings.StudioIntrospectionClientId
            ? (ResourceSettings.StudioIntrospectionClientId, ResourceSettings.StudioIntrospectionClientSecret)
            : (ResourceSettings.IntrospectionClientId, ResourceSettings.IntrospectionClientSecret);
        var client = host.CreateDefaultClient(handler);
        client.BaseAddress = Issuer;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(id)}:{Uri.EscapeDataString(secret)}")));
        return client;
    }

    public async Task<HttpStatusCode> Revoke(Browser browser, string refreshToken) =>
        (await browser.RevokeAs(ClientId, refreshToken)).Status;

    McpProduct Product(string audience, string scope)
    {
        var resource = host.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Resources.Single(_ => _.Audience == audience);
        resource.Scopes.Select(_ => _.Name).ShouldContain(scope);
        return new(resource.Indicators.Single(), resource.Audience, scope);
    }
}
