// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Configuration;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantDirectory;

public class when_the_callback_fails_in_development : given.a_tenant_directory
{
    void Establish()
    {
        // Deliberately bypass startup validation to prove failure never falls back to fixtures.
        options.Resources[0].TenantsCallback = new TenantsCallbackOptions { Url = "https://products.example/tenants" };
        handler.Status = HttpStatusCode.ServiceUnavailable;
    }

    Task Because() => Lookup();

    [Fact] void should_not_fall_back() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Unreachable));
}
