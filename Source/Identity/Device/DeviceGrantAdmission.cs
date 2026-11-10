// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Device;

/// <summary>Device admission requires complete recovery coverage, current immutable stamps, and live authority.</summary>
/// <param name="gate">The local recovery gate.</param>
/// <param name="log">The independent epoch authority.</param>
/// <param name="tokens">The operational token manager.</param>
/// <param name="authorizations">The operational authorization manager.</param>
/// <param name="fences">The primary fence authority.</param>
/// <param name="accounts">The active-account reader.</param>
internal sealed class DeviceGrantAdmission(RecoveryGate gate, IIndependentFenceLog log, IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations, IFenceStore fences, IAccountReader accounts)
{
    /// <summary>Checks complete readiness against the independent current epoch.</summary>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Whether device authorization is safe to admit.</returns>
    public async Task<bool> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var epoch = await log.RecoveryEpoch(cancellationToken);
            if (epoch != gate.Epoch)
            {
                gate.Close();
                gate.ObserveEpoch(epoch);
            }

            return gate.Ready && gate.Epoch == epoch;
        }
        catch (Exception)
        {
            gate.Close();
            return false;
        }
    }

    /// <summary>Validates a stored code, its immutable epoch, and (on redemption) its live grant and account.</summary>
    /// <param name="principal">The validated code principal.</param>
    /// <param name="redeeming">Whether the device is redeeming an approved code.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Whether admission is permitted.</returns>
    public async Task<bool> CanUse(ClaimsPrincipal principal, bool redeeming, CancellationToken cancellationToken)
    {
        try
        {
            if (!await Ready(cancellationToken)) return false;
            var epoch = gate.Epoch;
            if (!await CurrentToken(principal.GetTokenId(), epoch, cancellationToken)) return false;
            var bound = redeeming
                ? await CurrentGrant(principal, epoch, cancellationToken)
                : await CurrentToken(principal.GetClaim(Claims.Private.DeviceCodeId), epoch, cancellationToken);
            if (!bound) return false;

            return await Ready(cancellationToken) && gate.Epoch == epoch;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool Unfenced(GrantState? state, string grant, long epoch) => state is null ||
        (GrantStateValidation.IsKnown(state) && state.Fence is null && state.RecoveryEpoch == epoch && state.GrantId == grant);

    static bool HasCurrentStamp(ImmutableDictionary<string, JsonElement> properties, long epoch) =>
        properties.ContainsKey(RecoveryEpochStamp.PropertyName) && RecoveryEpochStamp.IsCurrent(properties, epoch);

    async Task<bool> CurrentToken(string? id, long epoch, CancellationToken cancellationToken)
    {
        var token = string.IsNullOrEmpty(id) ? null : await tokens.FindByIdAsync(id, cancellationToken);
        return token is not null && HasCurrentStamp(await tokens.GetPropertiesAsync(token, cancellationToken), epoch);
    }

    async Task<bool> CurrentGrant(ClaimsPrincipal principal, long epoch, CancellationToken cancellationToken)
    {
        var grant = principal.GetAuthorizationId();
        var subject = principal.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(grant) || !AccountSubject.TryParse(subject, out var account)) return false;
        var state = await fences.Read(grant, cancellationToken);
        if (!Unfenced(state, grant, epoch)) return false;
        var authorization = await authorizations.FindByIdAsync(grant, cancellationToken);
        if (authorization is null || !await CurrentAuthorization(authorization, subject!, epoch, cancellationToken)) return false;
        return await accounts.FindActive(account!, cancellationToken) is not null;
    }

    async Task<bool> CurrentAuthorization(object authorization, string subject, long epoch, CancellationToken cancellationToken) =>
        string.Equals(await authorizations.GetSubjectAsync(authorization, cancellationToken), subject, StringComparison.Ordinal) &&
        await authorizations.HasStatusAsync(authorization, Statuses.Valid, cancellationToken) &&
        HasCurrentStamp(await authorizations.GetPropertiesAsync(authorization, cancellationToken), epoch);
}
