// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.given;

public class MemoryRequests : IConsentRequestStore
{
    readonly Lock _gate = new();
    readonly Dictionary<ConsentRequestId, ConsentRequest> _rows = [];
    public ConsentRequestState? StopBefore;
    public bool LoseNextCompareExchange;

    public Task<bool> Create(ConsentRequest request, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_rows.TryAdd(request.Id, request));
    }

    public Task<ConsentRequest?> Read(ConsentRequestId id, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_rows.GetValueOrDefault(id));
    }

    public Task<bool> CompareExchange(ConsentRequest expected, ConsentRequest replacement, CancellationToken cancellationToken)
    {
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
                return Task.FromResult(false);
            }

            if (!_rows.TryGetValue(expected.Id, out var current) || current != expected) return Task.FromResult(false);
            if (!ConsentRequestTransitions.CanReplace(expected, replacement)) throw new InvalidOperationException("Invalid CAS");
            _rows[expected.Id] = replacement;
            return Task.FromResult(true);
        }
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
