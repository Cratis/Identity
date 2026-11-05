// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_with_another_content_type : given.a_tenants_callback
{
    void Establish() => handler.MediaType = "text/html";
    Task Because() => Lookup();

    [Fact] void should_be_malformed() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
}
