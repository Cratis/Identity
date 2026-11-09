// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_two_accounts_share_an_erasure_id : given.an_erasure_workflow
{
    AccountSubject _other;
    FactId _first;
    FactId _second;

    async Task Establish()
    {
        _other = AccountSubject.New();
        await Tombstone();
        await _log.Append(_other, new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New()));
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
    }

    async Task Because()
    {
        await Coordinator.Run(_account, _erasure);
        _first = _log.Facts.OfType<AccountErasureRecorded>().Single().FactId;
        _log.Facts.Clear();
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _other, _erasure);
        await Coordinator.Run(_other, _erasure);
        _second = _log.Facts.OfType<AccountErasureRecorded>().Single().FactId;
    }

    [Fact] void should_derive_distinct_publication_identities() => _first.ShouldNotEqual(_second);
}
