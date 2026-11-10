// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;
using Cratis.Identity.Sessions;

namespace Cratis.Identity.ConsentFlow.Deciding.given;

internal class MemoryRequests : IConsentRequestStore
{
    readonly Lock _gate = new();
    readonly Dictionary<ConsentRequestId, ConsentRequest> _rows = [];
    public ConsentRequestState? StopBefore;
    public bool LoseNextCompareExchange;
    public TimeProvider Clock = TimeProvider.System;
    public Func<ConsentRequest, ConsentRequest, Task>? BeforeCompareExchange;
    public readonly List<(ConsentRequest Expected, ConsentRequest Replacement, bool Won)> Attempts = [];

    public Task<bool> Create(ConsentRequest request, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_rows.TryAdd(request.Id, request));
    }

    public Task<ConsentRequest?> Read(ConsentRequestId id, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_rows.GetValueOrDefault(id));
    }

    public Task<ConsentRequest?> FindPending(AccountSubject account, SessionId session, RealmId realm, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.Values.Where(_ => _.Binding.Account == account && _.Binding.Session == session && _.Binding.Realm == realm &&
                _.State == ConsentRequestState.Pending && _.Deadline > now).OrderByDescending(_ => _.CreatedAt).FirstOrDefault());
        }
    }

    public async Task<bool> CompareExchange(ConsentRequest expected, ConsentRequest replacement, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (BeforeCompareExchange is not null) await BeforeCompareExchange(expected, replacement);
        lock (_gate)
        {
            if (StopBefore == replacement.State)
            {
                StopBefore = null;
                throw new InvalidOperationException("Simulated process stop");
            }

            if (LoseNextCompareExchange)
            {
                LoseNextCompareExchange = false;
                return Complete(expected, replacement, false);
            }

            if (!_rows.TryGetValue(expected.Id, out var current) || current != expected) return Complete(expected, replacement, false);
            if (!ConsentRequestTransitions.CanReplace(expected, replacement)) throw new InvalidOperationException("Invalid CAS");
            if (IsExpiredWrite(expected, replacement, now)) return Complete(expected, replacement, false);
            _rows[expected.Id] = replacement;
            return Complete(expected, replacement, true);
        }
    }

    bool IsExpiredWrite(ConsentRequest expected, ConsentRequest replacement, DateTimeOffset now) =>
        ((expected.State == ConsentRequestState.Pending && replacement.State is ConsentRequestState.Decided or ConsentRequestState.Denied) || replacement.State == ConsentRequestState.Active) &&
        (replacement.Deadline <= now || replacement.Deadline <= Clock.GetUtcNow());

    bool Complete(ConsentRequest expected, ConsentRequest replacement, bool won)
    {
        Attempts.Add((expected, replacement, won));
        return won;
    }

    public Task<IReadOnlyList<ConsentRequest>> Unfinished(int limit, ConsentRequestId? after, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<ConsentRequest>>(_rows.Values
                .Where(_ => (_.State is ConsentRequestState.Pending or ConsentRequestState.Decided or ConsentRequestState.EvidenceRecorded) &&
                    (after is null || string.CompareOrdinal(_.Id.Value.ToString("D"), after.Value.ToString("D")) > 0))
                .OrderBy(_ => _.Id.Value.ToString("D"), StringComparer.Ordinal).Take(limit).ToArray());
        }
    }
}
