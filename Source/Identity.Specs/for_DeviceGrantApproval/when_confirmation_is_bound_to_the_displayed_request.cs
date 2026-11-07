// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.for_DeviceGrantApproval;

public class when_confirmation_is_bound_to_the_displayed_request : Specification
{
    bool _original;
    bool[] _changed;
    bool _tampered;
    bool _expired;

    void Because()
    {
        var protection = new EphemeralDataProtectionProvider();
        var approval = new DeviceGrantApproval(protection);
        const string binding = "code-id\0account-id\0session-digest\0client-id\0audience\0scope-a\0tenant-a";
        var proof = approval.Create(binding);
        _original = approval.Matches(proof, binding);
        _changed = new[] { "code-id", "account-id", "session-digest", "client-id", "audience", "scope-a", "tenant-a" }
            .Select(value => approval.Matches(proof, binding.Replace(value, "changed", StringComparison.Ordinal))).ToArray();
        var tampered = proof[..10] + (proof[10] == 'A' ? 'B' : 'A') + proof[11..];
        _tampered = approval.Matches(tampered, binding);
        var expired = protection.CreateProtector("Cratis.Identity.DeviceApproval.v1").ToTimeLimitedDataProtector().Protect(binding, DateTimeOffset.UtcNow.AddMinutes(-1));
        _expired = approval.Matches(expired, binding);
    }

    [Fact] void should_accept_the_original_confirmation() => _original.ShouldBeTrue();
    [Fact] void should_reject_every_changed_security_binding() => _changed.All(_ => !_).ShouldBeTrue();
    [Fact] void should_reject_tampered_proofs() => _tampered.ShouldBeFalse();
    [Fact] void should_reject_expired_proofs() => _expired.ShouldBeFalse();
}
