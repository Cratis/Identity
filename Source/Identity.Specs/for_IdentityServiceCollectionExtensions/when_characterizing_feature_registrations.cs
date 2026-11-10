// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_characterizing_feature_registrations : given.registered_features
{
    string[] _enabled;
    string[] _disabled;

    // HOLD 9 prevented executing the pre-extraction state. Generate both baselines there before removing these skips.
    static string[] ExpectedEnabled => throw new InvalidOperationException("Device-enabled baseline has not been generated.");
    static string[] ExpectedDisabled => throw new InvalidOperationException("Device-disabled baseline has not been generated.");

    void Because()
    {
        _enabled = Snapshot(enabled_services, enabled_provider);
        _disabled = Snapshot(disabled_services, disabled_provider);
    }

    [Fact(Skip = "HOLD 9: generate the pre-extraction device-enabled descriptor baseline before enabling this assertion.")]
    void should_preserve_enabled_registrations_and_order_sensitive_subsequences() => _enabled.ShouldEqual(ExpectedEnabled);

    [Fact(Skip = "HOLD 9: generate the pre-extraction device-disabled descriptor baseline before enabling this assertion.")]
    void should_preserve_disabled_registrations_and_order_sensitive_subsequences() => _disabled.ShouldEqual(ExpectedDisabled);
}
