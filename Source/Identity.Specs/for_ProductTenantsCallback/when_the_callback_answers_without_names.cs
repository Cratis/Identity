// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_without_names : given.a_tenants_callback
{
    void Establish() => handler.Body = /*lang=json*/ """{"subject":"7d9d5c9e-0000-4000-8000-000000000001","tenants":[{"id":"one"},{"id":"two","name":" "}],"extra":true}""";
    Task Because() => Lookup();

    [Fact] void should_fall_back_to_the_id() => ((TenantsListed)result).Tenants.Select(_ => _.Name).ShouldContainOnly("one", "two");
}
