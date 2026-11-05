// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_lists_no_tenants : given.a_tenants_callback
{
    void Establish() => handler.Body = /*lang=json*/ """{"subject":"account-1","tenants":[]}""";
    Task Because() => Lookup();

    [Fact] void should_be_an_empty_success_not_an_outage() => ((TenantsListed)result).Tenants.ShouldBeEmpty();
}
