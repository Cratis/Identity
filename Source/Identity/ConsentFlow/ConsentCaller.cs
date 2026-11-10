// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Resolves only authenticated account, session and server-owned realm identifiers.</summary>
/// <param name="context">The authenticated HTTP context.</param>
/// <param name="sessions">The trusted session resolver.</param>
/// <param name="options">The issuer realm configuration.</param>
internal sealed class ConsentCaller(IHttpContextAccessor context, ICurrentConsentSession sessions, IOptions<IdentityOptions> options)
{
    /// <summary>Resolves a fully authenticated caller or fails closed.</summary>
    /// <returns>The caller, or null.</returns>
    internal ConsentCallerIdentity? Resolve()
    {
        var principal = context.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !AccountSubject.TryParse(principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier), out var account) ||
            string.IsNullOrWhiteSpace(sessions.SessionId) || string.IsNullOrWhiteSpace(options.Value.Issuer))
        {
            return null;
        }
        return new(account!, sessions.SessionId, options.Value.Issuer);
    }
}
