// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Durable request storage. Only the operational state, never evidence replay, enables a grant.</summary>
public interface IConsentRequestStore
{
    /// <summary>Inserts a new pending request without overwriting an existing identity.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether insertion won.</returns>
    Task<bool> Create(ConsentRequest request, CancellationToken cancellationToken);

    /// <summary>Reads the authoritative snapshot.</summary>
    /// <param name="id">The request identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapshot, or null.</returns>
    Task<ConsentRequest?> Read(ConsentRequestId id, CancellationToken cancellationToken);

    /// <summary>Replaces only the matching revision and state; preserves the identity and binding.</summary>
    /// <param name="expected">The observed snapshot.</param>
    /// <param name="replacement">The next snapshot with revision incremented by one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the compare-and-set won.</returns>
    Task<bool> CompareExchange(ConsentRequest expected, ConsentRequest replacement, CancellationToken cancellationToken);

    /// <summary>Reads unresolved work ordered by identity, allowing fair bounded cursor scans across retries.</summary>
    /// <param name="limit">The maximum number of rows.</param>
    /// <param name="after">The exclusive cursor, or null to begin a new scan.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapshots.</returns>
    Task<IReadOnlyList<ConsentRequest>> Unfinished(int limit, ConsentRequestId? after, CancellationToken cancellationToken);
}
