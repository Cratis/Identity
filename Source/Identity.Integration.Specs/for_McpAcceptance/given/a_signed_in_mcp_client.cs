// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Obtains separately consented Direct and Studio grants through one Identity sign-in.
/// A deployed harness can reuse this flow without changing its assertions.
/// </summary>
/// <param name="harness">The realm and registered client to exercise.</param>
public abstract class a_signed_in_mcp_client(IMcpAcceptanceHarness harness) : Specification
{
    protected McpResourceValidator Direct;
    protected McpResourceValidator Studio;
    protected JsonElement DirectTokens;
    protected JsonElement StudioTokens;
    protected ManualTimeProvider Time = new();
    protected ControlledIntrospection DirectIntrospection = new();
    protected ControlledIntrospection StudioIntrospection = new();
    Browser _browser;

    protected string DirectAccessToken => DirectTokens.GetProperty("access_token").GetString()!;
    protected string StudioAccessToken => StudioTokens.GetProperty("access_token").GetString()!;

    protected Task<HttpStatusCode> RevokeDirect() => harness.Revoke(_browser, DirectTokens.GetProperty("refresh_token").GetString());
    protected Task<HttpStatusCode> RevokeStudio() => harness.Revoke(_browser, StudioTokens.GetProperty("refresh_token").GetString());

    async Task Establish()
    {
        var browser = _browser = harness.CreateBrowser();
        Direct = await McpResourceValidator.Start(harness.Direct, harness.Issuer, harness.CreateIntrospectionClient(harness.Direct, DirectIntrospection), Time);
        Studio = await McpResourceValidator.Start(harness.Studio, harness.Issuer, harness.CreateIntrospectionClient(harness.Studio, StudioIntrospection), Time);
        var directAuthorization = await Direct.Discover(browser, harness.Direct);
        var studioAuthorization = await Studio.Discover(browser, harness.Studio);
        DirectTokens = await McpSignIn.Authorize(browser, harness, harness.Direct, directAuthorization);
        harness.PreventFurtherSignIns(browser);
        StudioTokens = await McpSignIn.Authorize(browser, harness, harness.Studio, studioAuthorization);
    }

    async Task Destroy()
    {
        _browser?.Client.Dispose();
        if (Direct is not null)
        {
            await Direct.DisposeAsync();
        }

        if (Studio is not null)
        {
            await Studio.DisposeAsync();
        }
    }
}
