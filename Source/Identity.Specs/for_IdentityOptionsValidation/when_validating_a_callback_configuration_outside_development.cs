// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_callback_configuration_outside_development : given.a_callback_configuration
{
    void Because() => Validate();

    [Fact] void should_succeed() => result.Succeeded.ShouldBeTrue();
}
