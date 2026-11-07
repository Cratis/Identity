// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>
/// The future Chronicle evidence boundary. Implementations must serialize Record and Resolve per request,
/// deduplicate by request identity, and reject appends after a NotRecorded resolution. A partial append is Unknown.
/// </summary>
public interface IConsentEvidence
{
    /// <summary>Idempotently records the complete evidence; acknowledgement loss must not permit a duplicate.</summary>
    /// <param name="request">The immutable request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The confirmed outcome, or Unknown.</returns>
    Task<ConsentEvidenceResolution> Record(ConsentRequest request, CancellationToken cancellationToken);

    /// <summary>Authoritatively resolves and seals an append against current and future writers.</summary>
    /// <param name="id">The request identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete append outcome; unavailable or partial outcomes are Unknown.</returns>
    Task<ConsentEvidenceResolution> Resolve(ConsentRequestId id, CancellationToken cancellationToken);
}
