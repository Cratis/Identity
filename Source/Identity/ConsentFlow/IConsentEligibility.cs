// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Revalidates catalogue inputs without becoming a session or revocation fence.</summary>
internal interface IConsentEligibility
{
    /// <summary>Rechecks account activity, the exact grant and the tenant directory.</summary>
    /// <param name="request">The immutable operational request.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The eligible presentation inputs, or null.</returns>
    Task<ConsentEligibility?> Evaluate(ConsentRequest request, CancellationToken cancellationToken);
}
