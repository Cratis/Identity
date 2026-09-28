// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds log messages for <see cref="TokenEndpoint"/>.
/// </summary>
internal static partial class TokenEndpointLogging
{
    [LoggerMessage(LogLevel.Warning, "Token request for authorization {AuthorizationId} refused: account {AccountId} no longer exists")]
    internal static partial void AccountMissing(this ILogger<TokenEndpoint> logger, string? authorizationId, string? accountId);

    [LoggerMessage(LogLevel.Warning, "Token request for authorization {AuthorizationId} refused: account {AccountId} can no longer select tenant {TenantId} for {Audience}")]
    internal static partial void TenantNoLongerSelectable(this ILogger<TokenEndpoint> logger, string? authorizationId, string accountId, string? tenantId, string audience);
}
