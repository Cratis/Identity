// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_rebuilding_restriction_intents : Specification
{
    OutboxEntry[] _original;
    OutboxEntry[] _rebuilt;

    void Because()
    {
        var fence = new GrantFence(Guid.NewGuid(), 1, "grant", Guid.NewGuid().ToString("D"), RevocationReason.RefreshTokenReused, DateTimeOffset.UnixEpoch);
        _original = FenceRecordFactory.Entries(fence);
        _rebuilt = FenceRecordFactory.Entries(fence);
    }

    [Fact] void should_keep_all_fact_ids_and_idempotency_keys() => _rebuilt.SequenceEqual(_original).ShouldBeTrue();
    [Fact] void should_keep_the_reuse_detection_id() => _rebuilt.Single(entry => entry.Kind == FactKind.Reuse).DetectionId.ShouldEqual(_original.Single(entry => entry.Kind == FactKind.Reuse).DetectionId);
}
