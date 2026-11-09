// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Options;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.DPoP;

/// <summary>Admits proofs before any grant consumption, failing closed on shared state failure.</summary>
/// <remarks>Only the enabled profile may register this optional refresh-handler dependency.</remarks>
/// <param name="validator">The cryptographic validator.</param>
/// <param name="nonces">The stateless nonce service.</param>
/// <param name="replay">The shared replay reservation store.</param>
/// <param name="binding">The request binding.</param>
/// <param name="options">The opt-in policy.</param>
/// <param name="time">The shared clock.</param>
[IgnoreConvention]
internal sealed class DPoPAdmission(DPoPProofValidator validator, DPoPNonce nonces, IDPoPReplayStore replay, DPoPBinding binding, IOptions<IdentityOptions> options, TimeProvider time)
{
    /// <summary>Checks a new grant, or a refresh entry's authoritative existing binding.</summary>
    /// <param name="context">The successful token sign-in context.</param>
    /// <param name="refresh">Whether this is an existing refresh family.</param>
    /// <param name="existingJkt">The persisted family binding.</param>
    /// <returns>Whether issuance may proceed.</returns>
    internal async Task<bool> Admit(ProcessSignInContext context, bool refresh = false, string? existingJkt = null)
    {
        binding.Jkt = null;
        var policy = options.Value.DPoP;
        var http = context.Transaction.GetHttpRequest();
        var values = http?.Headers["DPoP"];
        var headers = values is { Count: > 0 } ? values.Value.Select(value => value ?? string.Empty).ToArray() : [];
        var required = policy.RequireForAllClients || policy.RequiredClients.Contains(context.Request.ClientId ?? string.Empty, StringComparer.Ordinal);
        if (headers.Length == 0 && existingJkt is null && !required) return true;
        if (refresh && existingJkt is null) return Refuse(context, headers.Length > 0 ? DPoPFailure.UnboundFamily : DPoPFailure.MissingProof);
        var origin = new Uri(policy.PublicTokenEndpoint).GetLeftPart(UriPartial.Authority);
        var result = validator.Validate(headers, policy.PublicTokenEndpoint);
        if (result.Proof is not { } proof) return Refuse(context, result.Failure);
        if (existingJkt is not null && !string.Equals(existingJkt, proof.Jkt, StringComparison.Ordinal)) return Refuse(context, DPoPFailure.JktMismatch);
        if (!nonces.Validate(proof.Nonce, origin, proof.Jkt))
        {
            NonceHeader(context, nonces.Issue(origin, proof.Jkt));
            return Refuse(context, DPoPFailure.NonceRequired);
        }

        if (!await Reserve(context, origin, proof)) return false;

        binding.Jkt = proof.Jkt;
        NonceHeader(context, nonces.Issue(origin, proof.Jkt));
        return true;
    }

    static bool Refuse(ProcessSignInContext context, DPoPFailure reason)
    {
        var description = reason switch
        {
            DPoPFailure.MissingProof => "missing_proof",
            DPoPFailure.UnsupportedAlgorithm => "unsupported_alg",
            DPoPFailure.InvalidJwk => "invalid_jwk",
            DPoPFailure.InvalidSignature => "invalid_signature",
            DPoPFailure.HtmMismatch => "htm_mismatch",
            DPoPFailure.HtuMismatch => "htu_mismatch",
            DPoPFailure.IatOutsideWindow => "iat_outside_window",
            DPoPFailure.InvalidJti => "invalid_jti",
            DPoPFailure.NonceRequired => "nonce_required",
            DPoPFailure.JktMismatch => "jkt_mismatch",
            DPoPFailure.UnboundFamily => "unbound_family",
            DPoPFailure.ReusedJti => "reused_jti",
            DPoPFailure.JtiCacheUnavailable => "jti_cache_unavailable",
            _ => "malformed_proof"
        };
        context.Reject(reason == DPoPFailure.NonceRequired ? "use_dpop_nonce" : "invalid_dpop_proof", description);
        return false;
    }

    static void NonceHeader(ProcessSignInContext context, string nonce)
    {
        if (context.Transaction.GetHttpRequest()?.HttpContext.Response is { } response)
        {
            response.Headers["DPoP-Nonce"] = nonce;
            response.Headers.CacheControl = "no-store";
            response.Headers.Pragma = "no-cache";
        }
    }

    async Task<bool> Reserve(ProcessSignInContext context, string origin, DPoPProof proof)
    {
        try
        {
            return await replay.TryReserve("as", origin, proof.Jkt, proof.Jti, time.GetUtcNow().AddSeconds(390), context.CancellationToken) || Refuse(context, DPoPFailure.ReusedJti);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // No exception details: drivers and implementations may include untrusted inputs.
            return Refuse(context, DPoPFailure.JtiCacheUnavailable);
        }
    }
}
