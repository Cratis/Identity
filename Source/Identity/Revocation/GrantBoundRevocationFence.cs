// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow;

namespace Cratis.Identity.Revocation;

/// <summary>Conservative adapter for future consent wiring, not a claim that the session claim or the restore gate is complete.</summary>
/// <param name="store">The grant authority.</param>
internal sealed class GrantBoundRevocationFence(IFenceStore store) : IRevocationFence
{
    /// <inheritdoc/>
    public async Task<bool> IsClear(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.AuthorizationId))
        {
            return false;
        }

        try
        {
            var state = await store.Read(request.AuthorizationId, cancellationToken);
            if (state is null || state.Fence is not null || !GrantStateValidation.IsKnown(state))
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        // A clear grant alone cannot attest that an interactive session remains live. The existing slice has
        // no authoritative session/recovery gate; the real session claim must supply that evidence before permitting activation.
        // In particular, the co-located interim log here is not B1 and cannot attest restore safety.
        return false;
    }
}
