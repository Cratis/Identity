// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.for_ProductTenantsCallback.given;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_TenantDirectory.given;

public class a_tenant_directory : Specification
{
    protected IdentityOptions options;
    protected HostingEnvironment environment;
    protected CallbackHandler handler;
    private protected TenantLookup result;

    void Establish()
    {
        options = Configurations.TwoResourceServers();
        environment = new HostingEnvironment { EnvironmentName = "Development" };
        handler = new CallbackHandler();
    }

    protected async Task Lookup()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(TenantsCallbackHttpClient.Name).Returns(new HttpClient(handler));
        var monitor = options.AsMonitor();
        var directory = new TenantDirectory(monitor, environment, new ProductTenantsCallback(factory, NullLogger<ProductTenantsCallback>.Instance), new ConfiguredTenantDirectory(monitor));
        result = await directory.SelectableTenants(new Account("account-1", 42, "member", "Member"), new ResourceServer(Configurations.DirectAudience, "Direct", [], []));
    }
}
