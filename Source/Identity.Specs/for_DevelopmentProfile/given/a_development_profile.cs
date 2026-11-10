// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_DevelopmentProfile.given;

public class a_development_profile : Specification
{
    protected const string LocalIssuer = "http://localhost:5100/";
    protected const string StoreName = "localhost-5100";

    protected string _root;
    protected string _contentRoot;
    protected string _storeDirectory;
    protected Dictionary<string, string?> _settings;
    protected HostingEnvironment _environment;
    protected IdentityOptions _startupOptions;
    protected ServiceCollection _services;
    protected Exception _exception;
    protected string _dataProtectionPath;
    private protected DevelopmentKeyStoreRoot _custody;
    private protected List<DevelopmentKeyStoreRoot> _custodies;

    void Establish()
    {
        var parent = Path.Combine(Path.GetTempPath(), $"identity-dev-specs-{Guid.NewGuid():N}");
        _root = Path.Combine(parent, "data");
        _contentRoot = Path.Combine(parent, "content");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_contentRoot);
        DevelopmentProfile.EnsureParent(_root);
        _custodies = [];
        _storeDirectory = Path.Combine(_root, "cratis", "identity-dev", StoreName);
        _environment = new HostingEnvironment { EnvironmentName = "Development", ContentRootPath = _contentRoot };
        _services = [];
        _settings = new()
        {
            ["Identity:Issuer"] = LocalIssuer,
            ["Identity:Development:Enabled"] = "true",
            ["Identity:Development:Clients:0:ClientId"] = "local-claude",
            ["Identity:Development:Clients:0:DisplayName"] = "Claude (local)",
            ["Identity:Development:Clients:0:RedirectUris:0"] = "http://localhost/callback",
            ["Identity:Development:Clients:0:RedirectUris:1"] = "http://127.0.0.1/callback",
            ["Identity:Development:Resources:0:Audience"] = DevelopmentProfile.DirectMcpAudience,
            ["Identity:Development:Resources:0:DisplayName"] = "Direct MCP (local)",
            ["Identity:Development:Resources:0:Indicators:0"] = "http://localhost:5200/mcp",
            ["Identity:Development:Resources:0:Scopes:0:Name"] = "direct:mcp:read",
            ["Identity:Development:Resources:0:Scopes:0:Description"] = "Read through MCP.",
            ["Identity:Development:Resources:1:Audience"] = "studio-mcp",
            ["Identity:Development:Resources:1:DisplayName"] = "Studio MCP (local)",
            ["Identity:Development:Resources:1:Indicators:0"] = "http://localhost:5300/mcp",
            ["Identity:Development:Resources:1:Scopes:0:Name"] = "studio:mcp:read",
            ["Identity:Development:Resources:1:Scopes:0:Description"] = "Read through MCP."
        };
    }

    void Destroy()
    {
        foreach (var custody in _custodies)
        {
            custody.Dispose();
        }

        Directory.Delete(Directory.GetParent(_root).FullName, true);
    }

    protected IdentityOptions Startup() => _startupOptions = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build().GetSection("Identity").Get<IdentityOptions>() ?? new();

    protected void Apply(TimeProvider? time = null) => _exception = Catch.Exception(() =>
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build();
        Startup();
        var applied = DevelopmentProfile.Apply(configuration, _environment, _startupOptions, new DevelopmentKeyStore(_root, _contentRoot, time ?? TimeProvider.System));
        applied?.Register(_services);
        _dataProtectionPath = applied?.DataProtectionPath;
        _custody = applied?.Root;
        if (_custody is not null)
        {
            _custodies.Add(_custody);
        }
    });
}
