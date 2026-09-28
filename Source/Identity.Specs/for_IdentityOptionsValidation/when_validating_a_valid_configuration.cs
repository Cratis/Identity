// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_valid_configuration : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new IdentityOptionsValidation().Validate(null, Configurations.TwoResourceServers());

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
