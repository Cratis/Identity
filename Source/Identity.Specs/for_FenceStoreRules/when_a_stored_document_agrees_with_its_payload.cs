// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_a_stored_document_agrees_with_its_payload : Specification
{
    bool _clear;
    bool _fenced;

    void Because()
    {
        var clear = FenceStoreFixtures.Clear();
        var fenced = FenceStoreFixtures.Fenced(FenceStoreFixtures.Fence());
        _clear = FenceStoreRules.IsConsistent(clear, FenceStoreFixtures.GrantId, FenceStoreFixtures.StoredClear(clear));
        _fenced = FenceStoreRules.IsConsistent(fenced, FenceStoreFixtures.GrantId, FenceStoreFixtures.StoredFenced(fenced));
    }

    [Fact] void should_accept_a_clear_family() => _clear.ShouldBeTrue();
    [Fact] void should_accept_a_fenced_grant_with_undelivered_restrictions() => _fenced.ShouldBeTrue();
}
