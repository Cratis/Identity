// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Authorization;

namespace Cratis.Identity.RevocationSpecs;

public class when_epoch_metadata_is_checked : Specification
{
    static ImmutableDictionary<string, JsonElement> Stamp<T>(T value) =>
        ImmutableDictionary<string, JsonElement>.Empty.Add(RecoveryEpochStamp.PropertyName, JsonSerializer.SerializeToElement(value));

    [Fact] void should_accept_unstamped_legacy_rows_only_at_epoch_zero() => RecoveryEpochStamp.IsCurrent([], 0).ShouldBeTrue();
    [Fact] void should_deny_unstamped_rows_after_restore() => RecoveryEpochStamp.IsCurrent([], 1).ShouldBeFalse();
    [Fact] void should_accept_a_current_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(4L), 4).ShouldBeTrue();
    [Fact] void should_deny_an_old_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(3L), 4).ShouldBeFalse();
    [Fact] void should_deny_a_future_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(5L), 4).ShouldBeFalse();
    [Fact] void should_deny_a_negative_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(-1L), 0).ShouldBeFalse();
    [Fact] void should_deny_a_string_stamp() => RecoveryEpochStamp.IsCurrent(Stamp("4"), 4).ShouldBeFalse();
    [Fact] void should_deny_a_null_stamp() => RecoveryEpochStamp.IsCurrent(Stamp<object?>(null), 0).ShouldBeFalse();
    [Fact] void should_deny_an_overflowing_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(ulong.MaxValue), 0).ShouldBeFalse();
    [Fact] void should_deny_a_fractional_stamp() => RecoveryEpochStamp.IsCurrent(Stamp(4.5), 4).ShouldBeFalse();
}
