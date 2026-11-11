// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_enabling_oidc_without_an_issuer : given.an_oidc_configuration
{
    ValidateOptionsResult _result;

    void Establish()
    {
        options.Issuer = string.Empty;
        options.Oidc.IssuanceEnabled = true;
        options.Oidc.ConsentDecision = "ImplicitFencedPath";
    }

    void Because() => _result = Validate();

    [Fact] void should_fail_closed_even_in_development() => _result.Failed.ShouldBeTrue();
    [Fact] void should_require_a_configured_public_issuer() => _result.Failures.ShouldContain("Identity:Issuer must be configured when OpenID Connect issuance is enabled.");
}
