// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_short_callback_secret_outside_development : given.a_callback_configuration
{
    void Establish() => options.Resources[0].TenantsCallback.Secret = new string('s', 31);
    void Because() => Validate();

    [Fact] void should_fail() => result.Failed.ShouldBeTrue();
}
