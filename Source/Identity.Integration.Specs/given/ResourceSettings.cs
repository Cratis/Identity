// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Resource and introspection client overrides layered over the shipped host configuration by the integration host and its binding spec.
/// </summary>
public static class ResourceSettings
{
    /// <summary>
    /// The confidential resource server client that introspects the CLI's MCP-audience access tokens.
    /// </summary>
    public const string IntrospectionClientId = "direct-mcp";

    /// <summary>
    /// The test-only secret for the MCP introspection client.
    /// </summary>
    public const string IntrospectionClientSecret = "mcp-introspection-secret-for-specs";

    /// <summary>
    /// The confidential resource server client that introspects Studio MCP-audience access tokens.
    /// </summary>
    public const string StudioIntrospectionClientId = "studio-mcp";

    /// <summary>
    /// The test-only secret for the Studio MCP introspection client.
    /// </summary>
    public const string StudioIntrospectionClientSecret = "studio-mcp-introspection-secret-for-specs";

    /// <summary>
    /// Creates tenant fixtures for both Direct resources and appends the Studio and Stage resources.
    /// </summary>
    /// <param name="memberTenant">The first selectable Direct tenant.</param>
    /// <param name="otherMemberTenant">The second selectable Direct tenant.</param>
    /// <param name="memberId">The upstream account that belongs to both tenants.</param>
    /// <returns>The resource configuration overrides.</returns>
    public static Dictionary<string, string?> Create(string memberTenant, string otherMemberTenant, string memberId) => new()
    {
        ["Identity:IntrospectionClients:0:ClientId"] = IntrospectionClientId,
        ["Identity:IntrospectionClients:0:ClientSecret"] = IntrospectionClientSecret,
        ["Identity:IntrospectionClients:1:ClientId"] = StudioIntrospectionClientId,
        ["Identity:IntrospectionClients:1:ClientSecret"] = StudioIntrospectionClientSecret,
        ["Identity:Resources:0:Tenants:0:Id"] = memberTenant,
        ["Identity:Resources:0:Tenants:0:Name"] = "Cratis",
        ["Identity:Resources:0:Tenants:0:Members:0"] = memberId,
        ["Identity:Resources:0:Tenants:1:Id"] = otherMemberTenant,
        ["Identity:Resources:0:Tenants:1:Name"] = "Second tenant",
        ["Identity:Resources:0:Tenants:1:Members:0"] = memberId,
        ["Identity:Resources:1:Tenants:0:Id"] = memberTenant,
        ["Identity:Resources:1:Tenants:0:Name"] = "Cratis",
        ["Identity:Resources:1:Tenants:0:Members:0"] = memberId,
        ["Identity:Resources:1:Tenants:1:Id"] = otherMemberTenant,
        ["Identity:Resources:1:Tenants:1:Name"] = "Second tenant",
        ["Identity:Resources:1:Tenants:1:Members:0"] = memberId,
        ["Identity:Resources:2:Audience"] = "studio-api",
        ["Identity:Resources:2:DisplayName"] = "Cratis Studio",
        ["Identity:Resources:2:Indicators:0"] = "https://cratis.studio/mcp",
        ["Identity:Resources:2:Scopes:0:Name"] = "studio:read",
        ["Identity:Resources:2:Scopes:0:Description"] = "Read Studio",
        ["Identity:Resources:2:Tenants:0:Id"] = "studio",
        ["Identity:Resources:2:Tenants:0:AllowAllAccounts"] = "true",
        ["Identity:Resources:3:Audience"] = "stage-api",
        ["Identity:Resources:3:DisplayName"] = "Stage",
        ["Identity:Resources:3:Indicators:0"] = "https://stage.example/api",
        ["Identity:Resources:3:Scopes:0:Name"] = "stage:read",
        ["Identity:Resources:3:Scopes:0:Description"] = "Read Stage",
        ["Identity:Resources:3:TenantsCallback:Url"] = "https://products.example/tenants"
    };
}
