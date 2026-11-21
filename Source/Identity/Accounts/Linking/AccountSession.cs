// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Sessions;
using Cratis.Identity.Upstream;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>A validated, fresh existing-account session binding.</summary>
/// <param name="Account">The canonical immutable account subject.</param>
/// <param name="SessionId">The authenticated session reference.</param>
internal record AccountSession(AccountSubject Account, SessionId SessionId)
{
    /// <summary>Checks the trusted authenticated session before accepting a link ceremony.</summary>
    /// <param name="principal">The authenticated account principal.</param>
    /// <param name="time">The server clock.</param>
    /// <returns>The validated session binding.</returns>
    internal static AccountSession Require(ClaimsPrincipal principal, TimeProvider time)
    {
        AccountOperationRefused.Require(principal.Identity?.IsAuthenticated == true, "existing_account_proof_required");
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        AccountOperationRefused.Require(AccountSubject.TryParse(raw, out var subject), "existing_account_proof_required");
        var sid = principal.FindFirstValue("sid");
        AccountOperationRefused.Require(!string.IsNullOrEmpty(sid), "existing_account_proof_required");
        var authTime = principal.FindFirstValue("auth_time");
        AccountOperationRefused.Require(long.TryParse(authTime, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds), "fresh_existing_account_proof_required");
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        AccountOperationRefused.Require(seconds <= now && now - seconds <= 300, "fresh_existing_account_proof_required");
        return new(subject!, sid!);
    }

    /// <summary>
    /// Requires a current authenticated account session.
    /// </summary>
    /// <param name="principal">The existing account principal.</param>
    /// <param name="time">The trusted clock.</param>
    /// <returns>The verified session, or its refusal.</returns>
    internal static Result<AccountSession, ValidationResult> Verify(ClaimsPrincipal principal, TimeProvider time)
    {
        try
        {
            return Require(principal, time);
        }
        catch (AccountOperationRefused refusal)
        {
            return ValidationResult.Error(refusal.Message);
        }
    }

    /// <summary>
    /// Requires fresh upstream proof bound to the existing session.
    /// </summary>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <param name="sessionId">The existing session identifier.</param>
    /// <param name="now">The trusted current time.</param>
    /// <returns>A refusal, or null when the proof is fresh and session-bound.</returns>
    internal static ValidationResult? RequireProof(VerifiedAuthentication? authentication, string sessionId, DateTimeOffset now)
    {
        if (authentication is null || authentication.SessionId != sessionId)
        {
            return ValidationResult.Error("fresh_session_bound_upstream_proof_required");
        }

        var age = now - authentication.AuthTime;
        return age >= TimeSpan.Zero && age <= TimeSpan.FromMinutes(5)
            ? null
            : ValidationResult.Error("fresh_session_bound_upstream_proof_required");
    }
}
