// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds log messages for <see cref="RevokeGrantOnRefreshTokenRevocation"/>.
/// </summary>
internal static partial class RevokeGrantOnRefreshTokenRevocationLogging
{
    [LoggerMessage(LogLevel.Information, "Revoked authorization {AuthorizationId} and {Count} tokens after its refresh token was revoked")]
    internal static partial void GrantRevoked(this ILogger<RevokeGrantOnRefreshTokenRevocation> logger, string authorizationId, long count);
}
