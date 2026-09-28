// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_an_issuer_with_a_path : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = Configurations.TwoResourceServers();
        options.Issuer = "https://auth.example/identity/";
        _result = new IdentityOptionsValidation().Validate(null, options);
    }

    [Fact] void should_refuse_an_issuer_whose_discovery_would_be_under_a_different_path() => _result.Failed.ShouldBeTrue();
}
