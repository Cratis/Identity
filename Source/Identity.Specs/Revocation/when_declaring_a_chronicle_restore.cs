// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_declaring_a_chronicle_restore : given.a_recovery_pass
{
    void Establish()
    {
        _options.RestoreKind = RestoreKind.Chronicle;
        _options.RestoreId = "chronicle-restore-test";
        _options.RestrictionsReestablished = true;
    }

    async Task Because() => await Run();

    [Fact] void should_reset_restriction_delivery_markers() => _applied.Single().Reset.ShouldBeTrue();
    [Fact] void should_publish_the_durable_recovery_fence() => _facts.Received(1).Publish(_epochs.Single(), Arg.Any<CancellationToken>());
    [Fact] void should_rebind_the_restored_chronicle_store() => _facts.Received(1).Bind("test-custody", Arg.Any<CancellationToken>());
    [Fact] void should_open_only_with_explicit_uncovered_reconciliation() => _gate.Ready.ShouldBeTrue();
    [Fact] void should_never_enable_legacy_bootstrap_after_restore() => _gate.AllowLegacyBootstrap.ShouldBeFalse();
}
