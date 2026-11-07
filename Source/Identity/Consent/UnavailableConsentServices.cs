// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Fail-closed defaults until the evidence, fence and guarded authorization adapters are implemented.</summary>
internal sealed class UnavailableConsentServices : IConsentEvidence, IRevocationFence, IAuthorizationActivator
{
    /// <inheritdoc/>
    public Task<ConsentEvidenceResolution> Record(ConsentRequest request, CancellationToken cancellationToken) => Task.FromResult(ConsentEvidenceResolution.Unknown);

    /// <inheritdoc/>
    public Task<ConsentEvidenceResolution> Resolve(ConsentRequestId id, CancellationToken cancellationToken) => Task.FromResult(ConsentEvidenceResolution.Unknown);

    /// <inheritdoc/>
    public Task<bool> IsClear(ConsentRequest request, CancellationToken cancellationToken) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<string?> Prepare(ConsentRequest request, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
