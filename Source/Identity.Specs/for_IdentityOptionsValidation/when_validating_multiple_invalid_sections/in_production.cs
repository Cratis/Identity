// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation.when_validating_multiple_invalid_sections;

public class in_production : given.a_multi_invalid_configuration
{
    void Because() => Validate("Production");

    [Fact] void should_preserve_every_failure_in_section_and_resource_order() => result.Failures.ShouldEqual(Expected(false));
}
