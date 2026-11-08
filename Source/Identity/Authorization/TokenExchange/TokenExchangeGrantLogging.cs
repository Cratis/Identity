// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Holds log messages for <see cref="TokenExchangeGrant"/>. Token and claim values are never logged.
/// </summary>
internal static partial class TokenExchangeGrantLogging
{
    [LoggerMessage(LogLevel.Warning, "Token exchange by client {ClientId} refused: {Refusal}")]
    internal static partial void Refused(this ILogger<TokenExchangeGrant> logger, string clientId, SubjectTokenRefusal refusal);

    [LoggerMessage(LogLevel.Warning, "Token exchange by client {ClientId} for configured issuer {Issuer} refused: {Refusal} ({Detail})")]
    internal static partial void RefusedForIssuer(this ILogger<TokenExchangeGrant> logger, string clientId, SubjectTokenRefusal refusal, string issuer, string detail);

    [LoggerMessage(LogLevel.Warning, "Token exchange by client {ClientId}: tenant selection unavailable for account {AccountId} on {Audience}")]
    internal static partial void TenantsUnavailable(this ILogger<TokenExchangeGrant> logger, string clientId, string accountId, string audience);

    [LoggerMessage(LogLevel.Warning, "Token exchange by client {ClientId} refused: account {AccountId} cannot select tenant {TenantId} on {Audience}")]
    internal static partial void TenantNotSelectable(this ILogger<TokenExchangeGrant> logger, string clientId, string accountId, string tenantId, string audience);
}
