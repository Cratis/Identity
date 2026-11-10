// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinkState;

public class when_inspecting_the_decision_state : Specification
{
    bool _isPassive;

    void Because() => _isPassive = Attribute.IsDefined(typeof(AccountLinkState), typeof(PassiveAttribute), inherit: false);

    [Fact]
    void should_be_passive_so_the_guarded_read_is_current_and_never_a_stale_materialization() => _isPassive.ShouldBeTrue();
}
