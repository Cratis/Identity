// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_an_indicator_with_a_fragment : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = Configurations.TwoResourceServers();
        options.Resources[0].Indicators.Add("https://cratis.direct/mcp#fragment");
        _result = new IdentityOptionsValidation().Validate(null, options);
    }

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
}
