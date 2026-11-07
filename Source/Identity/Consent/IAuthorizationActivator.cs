// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>
/// Prepares an unusable authorization, idempotently keyed by ConsentRequestId. A production adapter MUST guard
/// every issuance/redemption with the operational Active state and a live revocation fence. Preparation alone,
/// evidence replay, and possession of the returned identity never confer authority. No production adapter is wired yet.
/// </summary>
public interface IAuthorizationActivator
{
    /// <summary>Prepares or finds the same dormant authorization; null means activation is unavailable.</summary>
    /// <param name="request">The evidenced request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stable authorization identity, or null.</returns>
    Task<string?> Prepare(ConsentRequest request, CancellationToken cancellationToken);
}
