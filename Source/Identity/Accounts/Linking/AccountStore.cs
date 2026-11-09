// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The guarded event-store boundary; no public raw append operation.</summary>
/// <param name="eventStore">The account event store.</param>
/// <param name="readModels">The passive account fold reader.</param>
/// <param name="time">The trusted append clock.</param>
internal class AccountStore(IEventStore eventStore, IReadModels readModels, TimeProvider time)
{
    /// <summary>Initializes a new instance of the <see cref="AccountStore"/> class using its store's passive reader.</summary>
    /// <param name="eventStore">The account event store.</param>
    public AccountStore(IEventStore eventStore) : this(eventStore, eventStore.ReadModels, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountStore"/> class with a passive reader.
    /// </summary>
    /// <param name="eventStore">The account event store.</param>
    /// <param name="readModels">The passive account fold reader.</param>
    public AccountStore(IEventStore eventStore, IReadModels readModels) : this(eventStore, readModels, TimeProvider.System)
    {
    }

    /// <summary>Acquires the account-wide token before folding decision state.</summary>
    /// <param name="account">The immutable account source.</param>
    /// <returns>The guarded decision.</returns>
    internal async Task<GuardedAccountRead> Read(AccountSubject account)
    {
        // Acquire the projection token BEFORE reducing state. An intervening event makes the append conflict.
        var guard = await eventStore.GetDecisionReads().GetDetached<AccountGuard>(new ReadModelKey(account.Value));
        var state = await readModels.GetInstanceById<AccountLinkState>(new ReadModelKey(account.Value)) ?? new(account.Value);
        return new(guard, state);
    }

    /// <summary>Appends an Internal batch guarded by the acquired source token.</summary>
    /// <param name="account">The account source.</param>
    /// <param name="read">The guarded decision.</param>
    /// <param name="events">The facts to append.</param>
    /// <returns>The append completion.</returns>
    /// <exception cref="AccountOperationRefused">A kernel guard or uniqueness constraint refuses the batch.</exception>
    internal async Task Append(AccountSubject account, GuardedAccountRead read, IEnumerable<object> events)
    {
        if (!await TryAppend(account, read, events))
        {
            throw new AccountOperationRefused("upstream_key_already_reserved");
        }
    }

    /// <summary>
    /// Attempts the guarded batch, returning a uniqueness conflict without partially appending it.
    /// </summary>
    /// <param name="account">The account source.</param>
    /// <param name="read">The guarded decision.</param>
    /// <param name="events">The facts to append.</param>
    /// <returns>Whether the batch committed.</returns>
    /// <exception cref="AccountOperationRefused">The account changed after the guarded read.</exception>
    internal async Task<bool> TryAppend(AccountSubject account, GuardedAccountRead read, IEnumerable<object> events)
    {
        var batch = events.ToArray();
        var result = await eventStore.EventLog.AppendMany(batch.Select(@event => new EventForEventSourceId(account.Value, @event) { Occurred = time.GetUtcNow() }), [read.Guard]);
        if (!result.IsSuccess && !result.HasConstraintViolations)
        {
            throw new AccountOperationRefused("account_concurrency_conflict");
        }

        return result.IsSuccess;
    }

    /// <summary>Pairs a source token with a subsequent decision fold.</summary>
    /// <param name="Guard">The source concurrency guard.</param>
    /// <param name="State">The account decision state.</param>
    internal record GuardedAccountRead(DecisionRead<AccountGuard> Guard, AccountLinkState State);
}
