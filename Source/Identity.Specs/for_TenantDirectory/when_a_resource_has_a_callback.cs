// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantDirectory;

public class when_a_resource_has_a_callback : given.a_tenant_directory
{
    void Establish()
    {
        // Deliberately bypass startup validation to verify callback priority after a reload.
        options.Resources[0].TenantsCallback = new TenantsCallbackOptions { Url = "https://products.example/tenants" };
    }
    Task Because() => Lookup();

    [Fact] void should_use_only_the_callback_even_in_development() => ((TenantsListed)result).Tenants.Select(_ => _.Id).ShouldContainOnly("one", "two");
}
