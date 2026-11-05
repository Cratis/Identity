// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The guarded event-store boundary; no public raw append operation.</summary>
/// <param name="eventStore">The account event store.</param>
public class AccountStore(IEventStore eventStore)
{
    internal async Task<GuardedAccountRead> Read(AccountSubject account)
    {
        // Acquire the projection token BEFORE reducing state. An intervening event makes the append conflict.
        var guard = await eventStore.GetDecisionReads().GetDetached<AccountGuard>(new ReadModelKey(account.Value));
        var state = await eventStore.ReadModels.GetInstanceById<AccountLinkState>(account.Value) ?? new(account.Value);
        return new(guard, state);
    }

    internal async Task Append(AccountSubject account, GuardedAccountRead read, IEnumerable<object> events)
    {
        var batch = events.ToArray();
        if (batch.Length == 0)
        {
            return;
        }
        var result = await eventStore.EventLog.AppendMany(batch.Select(@event => new EventForEventSourceId(account.Value, @event)), [read.Guard]);
        if (!result.IsSuccess)
        {
            throw new AccountOperationRefused(result.HasConstraintViolations ? "upstream_key_already_reserved" : "account_concurrency_conflict");
        }
    }

    internal record GuardedAccountRead(DecisionRead<AccountGuard> Guard, AccountLinkState State);
}
