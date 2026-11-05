// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_development_callback : given.a_callback_configuration
{
    void Establish()
    {
        environment.EnvironmentName = "Development";
        options.Resources[0].TenantsCallback.Url = "http://localhost/tenants";
        options.Resources[0].TenantsCallback.Secret = string.Empty;
    }

    void Because() => Validate();

    [Fact] void should_allow_http_and_an_empty_secret() => result.Succeeded.ShouldBeTrue();
}
