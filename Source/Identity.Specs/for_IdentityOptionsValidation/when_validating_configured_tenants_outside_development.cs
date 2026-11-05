// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_configured_tenants_outside_development : given.a_callback_configuration
{
    void Establish() => options.Resources[0].Tenants.Add(new TenantOptions { Id = "fixture" });
    void Because() => Validate();

    [Fact] void should_refuse_fixtures() => result.Failures.Any(_ => _.Contains("configured tenants are development fixtures", StringComparison.Ordinal)).ShouldBeTrue();
}
