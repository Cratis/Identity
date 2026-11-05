// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_with_duplicate_tenants : given.a_tenants_callback
{
    void Establish() => handler.Body = /*lang=json*/ """{"subject":"account-1","tenants":[{"id":"one"},{"id":"one"}]}""";
    Task Because() => Lookup();

    [Fact] void should_be_malformed() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
}
