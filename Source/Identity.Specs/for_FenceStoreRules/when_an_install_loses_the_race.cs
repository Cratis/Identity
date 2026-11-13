// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_an_install_loses_the_race : Specification
{
    RedeemOutcome _missing;
    RedeemOutcome _fenced;
    RedeemOutcome _moved;
    RedeemOutcome _sameTokenNewerVersion;

    void Because()
    {
        _missing = FenceStoreRules.AfterLostRace(null, FenceStoreFixtures.TokenId);
        _fenced = FenceStoreRules.AfterLostRace(FenceStoreFixtures.Fenced(FenceStoreFixtures.Fence()), FenceStoreFixtures.TokenId);
        _moved = FenceStoreRules.AfterLostRace(FenceStoreFixtures.Clear(version: 2, tokenId: "successor"), FenceStoreFixtures.TokenId);
        _sameTokenNewerVersion = FenceStoreRules.AfterLostRace(FenceStoreFixtures.Clear(version: 2), FenceStoreFixtures.TokenId);
    }

    [Fact] void should_be_unknown_when_the_grant_vanished() => _missing.ShouldEqual(RedeemOutcome.Unknown);
    [Fact] void should_be_fenced_when_a_restriction_won() => _fenced.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_be_not_current_when_another_redeemer_won() => _moved.ShouldEqual(RedeemOutcome.NotCurrent);
    [Fact] void should_be_a_version_conflict_when_the_token_is_still_current() => _sameTokenNewerVersion.ShouldEqual(RedeemOutcome.VersionConflict);
}
