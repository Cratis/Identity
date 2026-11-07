// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Lazy repair is allowed only from a live, validated legacy entry and live authorization.</summary>
/// <param name="fences">The primary authority.</param>
/// <param name="tokens">The entry manager.</param>
/// <param name="authorizations">The authorization manager.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="gate">The host continuity guard.</param>
internal sealed class ReadRefreshGrant(IFenceStore fences, IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations, TimeProvider timeProvider, RecoveryGate? gate = null)
{
    /// <summary>Reads or atomically repairs once; a tombstone or unknown state is never repaired into permission.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="entryId">The validated entry.</param>
    /// <param name="entry">The reloaded OpenIddict entry.</param>
    /// <param name="principal">The validated principal.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The authoritative state, or denial.</returns>
    internal async Task<GrantState?> Read(string grantId, string entryId, object entry, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var state = await fences.Read(grantId, cancellationToken);
        if (state is not null)
        {
            return Admissible(state) ? state : null;
        }

        if (gate?.AllowLegacyBootstrap != true)
        {
            return null;
        }

        var subject = principal.GetClaim(Claims.Subject);
        var authorization = await authorizations.FindByIdAsync(grantId, cancellationToken);
        if (!AccountSubject.TryParse(subject, out _) || authorization is null ||
            !await authorizations.HasStatusAsync(authorization, Statuses.Valid, cancellationToken) ||
            await authorizations.GetSubjectAsync(authorization, cancellationToken) != subject ||
            !await tokens.HasStatusAsync(entry, Statuses.Valid, cancellationToken) ||
            await tokens.GetCreationDateAsync(entry, cancellationToken) is not { } created ||
            await tokens.GetExpirationDateAsync(entry, cancellationToken) is not { } expires || expires <= timeProvider.GetUtcNow())
        {
            return null;
        }

        return await fences.RecordFamilyStart(grantId, entryId, subject!, created, expires, cancellationToken);
    }

    bool Admissible(GrantState state)
    {
        if (!GrantStateValidation.IsKnown(state))
        {
            return false;
        }

        return gate is null || (gate.Ready && state.RecoveryEpoch == gate.Epoch);
    }
}
