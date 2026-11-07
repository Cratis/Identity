// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.given;

public class MemoryEvidence : IConsentEvidence
{
    readonly Lock _gate = new();
    readonly HashSet<ConsentRequestId> _recorded = [];
    readonly HashSet<ConsentRequestId> _sealed = [];
    public bool LoseAcknowledgement;
    public bool Unavailable;
    public int Appends;
    public Func<Task>? BeforeRecord;

    public async Task<ConsentEvidenceResolution> Record(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (BeforeRecord is not null) await BeforeRecord();
        lock (_gate)
        {
            if (Unavailable) return ConsentEvidenceResolution.Unknown;
            if (_sealed.Contains(request.Id)) return ConsentEvidenceResolution.NotRecorded;
            if (_recorded.Add(request.Id)) Appends++;
            var result = LoseAcknowledgement ? ConsentEvidenceResolution.Unknown : ConsentEvidenceResolution.Recorded;
            LoseAcknowledgement = false;
            return result;
        }
    }

    public Task<ConsentEvidenceResolution> Resolve(ConsentRequestId id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Unavailable) return Task.FromResult(ConsentEvidenceResolution.Unknown);
            if (_recorded.Contains(id)) return Task.FromResult(ConsentEvidenceResolution.Recorded);
            _sealed.Add(id);
            return Task.FromResult(ConsentEvidenceResolution.NotRecorded);
        }
    }
}
