// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantDirectory;

public class when_a_resource_has_no_callback_in_development : given.a_tenant_directory
{
    Task Because() => Lookup();

    [Fact] void should_use_fixtures() => ((TenantsListed)result).Tenants.Select(_ => _.Id).ShouldContainOnly("public");
    [Fact] void should_not_call_the_product() => handler.Requests.ShouldBeEmpty();
}
