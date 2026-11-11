// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_the_oidc_issuance_gate : given.an_oidc_configuration
{
    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "", false)]
    [InlineData(true, "implicitfencedpath", false)]
    [InlineData(true, "Unknown", false)]
    [InlineData(true, "ImplicitFencedPath", true)]
    [InlineData(true, "ActivationBarrier", true)]
    void should_require_a_recorded_decision_before_opening(bool enabled, string decision, bool valid)
    {
        options.Issuer = "https://identity.example/";
        options.Oidc.IssuanceEnabled = enabled;
        options.Oidc.ConsentDecision = decision;
        Validate().Succeeded.ShouldEqual(valid);
    }
}
