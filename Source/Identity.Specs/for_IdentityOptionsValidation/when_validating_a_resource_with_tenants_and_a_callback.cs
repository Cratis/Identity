// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_resource_with_tenants_and_a_callback : given.a_callback_configuration
{
    void Establish()
    {
        environment.EnvironmentName = "Development";
        options.Resources[0].Tenants.Add(new TenantOptions { Id = "fixture" });
    }

    void Because() => Validate();

    [Fact] void should_fail_even_in_development() => result.Failed.ShouldBeTrue();
}
