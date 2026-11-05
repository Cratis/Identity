// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback.given;

public class a_tenants_callback : Specification
{
    protected CallbackHandler handler;
    private protected ProductTenantsCallback callback;
    private protected CallbackLogger<ProductTenantsCallback> logger;
    protected Account account;
    protected ResourceServer resource;
    protected TenantsCallbackOptions options;
    protected TenantLookup result;

    void Establish()
    {
        handler = new CallbackHandler();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(TenantsCallbackHttpClient.Name).Returns(_ => new HttpClient(handler, disposeHandler: false));
        logger = new CallbackLogger<ProductTenantsCallback>();
        callback = new ProductTenantsCallback(factory, logger);
        account = new Account("account-1", 501, "member", "Member");
        resource = new ResourceServer("stage-api", "Stage", [], []);
        options = new TenantsCallbackOptions { Url = "https://products.example/tenants", Secret = new string('s', 32), Timeout = TimeSpan.FromMilliseconds(200) };
    }

    protected Task Lookup() => Lookup(CancellationToken.None);

    protected async Task Lookup(CancellationToken cancellationToken) => result = await callback.SelectableTenants(account, resource, options, cancellationToken);
}
