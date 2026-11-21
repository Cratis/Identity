// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinkState;

public class when_requiring_an_active_account : Specification
{
    const string Refusal = "account_not_active";

    ValidationResult? _missing;
    ValidationResult? _unregistered;
    ValidationResult? _tombstoned;
    ValidationResult? _disabled;
    ValidationResult? _active;

    void Because()
    {
        _missing = AccountLinkState.RequireActive(null);
        _unregistered = AccountLinkState.RequireActive(new("a"));
        _tombstoned = AccountLinkState.RequireActive(new("a") { Registered = true, Tombstoned = true });
        _disabled = AccountLinkState.RequireActive(new("a") { Registered = true, Disabled = true });
        _active = AccountLinkState.RequireActive(new("a") { Registered = true });
    }

    [Fact] void should_refuse_missing_state() => _missing!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_unregistered_account() => _unregistered!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_tombstoned_account() => _tombstoned!.Message.ShouldEqual(Refusal);
    [Fact] void should_refuse_disabled_account() => _disabled!.Message.ShouldEqual(Refusal);
    [Fact] void should_accept_active_account() => _active.ShouldBeNull();
}
