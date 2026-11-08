// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Holds log messages for <see cref="ExternalIssuerKeys"/>.
/// </summary>
internal static partial class ExternalIssuerKeysLogging
{
    [LoggerMessage(LogLevel.Warning, "Fetching the signing keys of token-exchange issuer {Issuer} failed: {Reason}")]
    internal static partial void KeysFetchFailed(this ILogger<ExternalIssuerKeys> logger, string issuer, string reason);
}
