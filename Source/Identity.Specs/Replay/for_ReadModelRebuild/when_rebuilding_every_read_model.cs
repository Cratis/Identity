// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Replay.given;

namespace Cratis.Identity.Replay.for_ReadModelRebuild;

public class when_rebuilding_every_read_model : Specification
{
    readonly ReadModelRebuild _rebuild = new();
    string[] _first;
    string[] _second;
    Account _account;
    Account _other;
    AccountLinkState _state;
    UpstreamKeyLookup _lookup;

    async Task Because()
    {
        _first = await _rebuild.Rebuild();
        _second = await _rebuild.Rebuild();
        _account = await _rebuild.Build<Account>();
        _other = await _rebuild.Build<Account>(true);
        _state = _rebuild.Reduce();
        _lookup = await _rebuild.Build<UpstreamKeyLookup>();
    }

    [Fact] void should_rebuild_every_model_deterministically_from_empty() => _second.ShouldEqual(_first);
    [Fact] void should_preserve_the_final_tombstone() => _account.Status.ShouldEqual(AccountStatus.Tombstoned);
    [Fact] void should_preserve_the_reducer_tombstone() => _state.Tombstoned.ShouldBeTrue();
    [Fact] void should_not_restore_the_unlinked_membership() => _account.LinkedUpstreamKeys.ShouldBeEmpty();
    [Fact] void should_keep_other_accounts_active() => _other.Status.ShouldEqual(AccountStatus.Active);
    [Fact] void should_not_unlink_the_other_accounts_key() => _other.LinkedUpstreamKeys.Count().ShouldEqual(1);
    [Fact] void should_keep_the_unlinked_key_reserved_but_inactive() => _lookup.Keys.Single().Active.ShouldBeFalse();
}
