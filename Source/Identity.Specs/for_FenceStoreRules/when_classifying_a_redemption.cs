// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_classifying_a_redemption : Specification
{
    RedeemOutcome? _installable;
    RedeemOutcome? _notCurrent;
    RedeemOutcome? _versionConflict;
    RedeemOutcome? _bothWrong;
    bool _fenced;
    bool _clearIsFenced;
    bool _sameEpoch;
    bool _otherEpoch;

    void Because()
    {
        var current = FenceStoreFixtures.Clear(version: 3, epoch: 2);
        _installable = FenceStoreRules.Refusal(current, FenceStoreFixtures.TokenId, 3);
        _notCurrent = FenceStoreRules.Refusal(current, "other", 3);
        _versionConflict = FenceStoreRules.Refusal(current, FenceStoreFixtures.TokenId, 2);
        _bothWrong = FenceStoreRules.Refusal(current, "other", 2);
        _fenced = FenceStoreRules.IsFenced(FenceStoreFixtures.Fenced(FenceStoreFixtures.Fence()));
        _clearIsFenced = FenceStoreRules.IsFenced(current);
        _sameEpoch = FenceStoreRules.IsStaleEpoch(current, 2);
        _otherEpoch = FenceStoreRules.IsStaleEpoch(current, 3);
    }

    [Fact] void should_allow_the_current_token_at_the_expected_version() => _installable.ShouldBeNull();
    [Fact] void should_refuse_a_token_that_is_not_current() => _notCurrent.ShouldEqual(RedeemOutcome.NotCurrent);
    [Fact] void should_refuse_a_stale_version() => _versionConflict.ShouldEqual(RedeemOutcome.VersionConflict);
    [Fact] void should_report_not_current_before_version_conflict() => _bothWrong.ShouldEqual(RedeemOutcome.NotCurrent);
    [Fact] void should_treat_a_fence_as_fenced() => _fenced.ShouldBeTrue();
    [Fact] void should_not_treat_a_clear_state_as_fenced() => _clearIsFenced.ShouldBeFalse();
    [Fact] void should_accept_the_same_epoch() => _sameEpoch.ShouldBeFalse();
    [Fact] void should_refuse_another_epoch() => _otherEpoch.ShouldBeTrue();
}
