// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>
/// Holds log messages for <see cref="LegacyAccountStoreGuard"/>. Only fixed text and exception type names are logged,
/// never connection strings, hosts, database names, driver messages or document counts.
/// </summary>
internal static partial class LegacyAccountStoreGuardLogging
{
    [LoggerMessage(LogLevel.Warning, "The legacy MongoDB account store check was skipped because the operator confirmed there is no legacy store")]
    internal static partial void CheckSkipped(this ILogger<LegacyAccountStoreGuard> logger);

    [LoggerMessage(LogLevel.Critical, "{Message}")]
    internal static partial void Refused(this ILogger<LegacyAccountStoreGuard> logger, string message);

    [LoggerMessage(LogLevel.Critical, "{Message} (failure type: {FailureType})")]
    internal static partial void RefusedUnverifiable(this ILogger<LegacyAccountStoreGuard> logger, string message, string failureType);
}
