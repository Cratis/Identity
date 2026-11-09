// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_HostedSharedConfiguration.given;

public class a_hosted_studio_configuration : Specification
{
    protected IConfiguration _configuration;

    protected virtual string? StudioIndicator => null;

    protected virtual string? StudioUrl => "https://studio.example/tenants";

    protected virtual string? StudioSecret => new('y', 32);

    void Establish()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:SecretStore:Path"] = "/run/identity/keys",
            ["Identity:GitHub:ClientId"] = "synthetic-client",
            ["Identity:GitHub:ClientSecret"] = "synthetic-upstream-secret",
            ["Identity:MongoDB:ConnectionString"] = "mongodb://operational.example",
            ["Identity:MongoDB:Database"] = "identity-products",
            ["Identity:Accounts:LookupKey"] = Convert.ToBase64String(new byte[32]),
            ["Identity:SecurityOwners:0"] = "synthetic-owner",
            ["Identity:FenceLog:ConnectionString"] = "mongodb://independent.example",
            ["Identity:FenceLog:Database"] = "fences",
            ["Identity:FenceLog:InstanceId"] = "synthetic-custody",
            ["Identity:FenceLog:MinimumSequence"] = "0",
            ["Cratis:Chronicle:ConnectionString"] = "chronicle://chronicle.example:35000",
            ["Identity:Resources:0:TenantsCallback:Url"] = "https://direct.example/tenants",
            ["Identity:Resources:0:TenantsCallback:Secret"] = new string('x', 32),
            ["Identity:Resources:1:TenantsCallback:Url"] = "https://direct.example/tenants",
            ["Identity:Resources:1:TenantsCallback:Secret"] = new string('x', 32),
            ["Identity:Resources:2:TenantsCallback:Url"] = StudioUrl,
            ["Identity:Resources:2:TenantsCallback:Secret"] = StudioSecret
        };
        if (StudioIndicator is not null)
        {
            settings["Identity:Resources:2:Indicators:0"] = StudioIndicator;
        }

        _configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json")
            .AddInMemoryCollection(settings).Build();
    }

    protected IdentityOptions Bound()
    {
        var options = new IdentityOptions();
        _configuration.GetSection("Identity").Bind(options);
        return options;
    }

    protected ValidateOptionsResult Validate()
    {
        var options = Bound();
        return new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
    }
}
