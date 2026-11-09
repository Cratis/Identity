// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// The Chronicle-backed account stream of the erasure workflow. Every append is a one-time append keyed on the
/// erasure (and, for failures, the failure ordinal), so concurrent or retried coordinators cannot duplicate a
/// request, a failure transition or the completion. There is deliberately no plain-append fallback: a kernel
/// that cannot honor keyed appends makes the log refuse.
/// </summary>
/// <param name="eventStore">The account event store of one realm or tenant namespace.</param>
/// <param name="time">The trusted append clock.</param>
internal class ChronicleAccountErasureLog(IEventStore eventStore, TimeProvider time) : IAccountErasureLog
{
    /// <inheritdoc/>
    public async Task<ErasureState> Read(AccountSubject account) => ErasureState.Fold(await Stream(account));

    /// <inheritdoc/>
    public async Task Append(AccountSubject account, object fact)
    {
        var sequence = eventStore.EventLog;
        AccountOperationRefused.Require(sequence.SupportsIdempotentAppends, "erasure_log_unavailable");
        var key = await KeyFor(account, fact);
        var result = await sequence.AppendOnce(new EventSourceId(account.Value), fact, new IdempotencyKey(key), occurred: time.GetUtcNow());

        // In 19.111.0 IsSuccess does not include HasIdempotencyConflicts; check both explicitly.
        AccountOperationRefused.Require(!result.HasIdempotencyConflicts, "erasure_concurrency_conflict");
        AccountOperationRefused.Require(result.IsSuccess, "erasure_log_unavailable");
    }

    async Task<IEnumerable<object>> Stream(AccountSubject account) =>
        (await eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, new EventSourceId(account.Value))).Select(@event => @event.Content);

    async Task<string> KeyFor(AccountSubject account, object fact) => fact switch
    {
        AccountErasureRequested requested => $"erasure-requested:{account.Value}:{requested.ErasureId.Value:D}",
        AccountErasureRecorded recorded => $"erasure-recorded:{account.Value}:{recorded.ErasureId.Value:D}",
        AccountErasureFailed failed => $"erasure-failed:{account.Value}:{failed.ErasureId.Value:D}:{(await Stream(account)).OfType<AccountErasureFailed>().Count(prior => prior.ErasureId == failed.ErasureId)}",
        _ => throw new ArgumentException("Only account erasure facts may be appended.", nameof(fact))
    };
}
