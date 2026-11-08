// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

internal static partial class ConsentReconcilerLogging
{
    [LoggerMessage(LogLevel.Warning, "Consent request {RequestId} remains unresolved in {State}")]
    internal static partial void RequestUnresolved(this ILogger<ConsentReconciler> logger, Guid requestId, ConsentRequestState state);

    [LoggerMessage(LogLevel.Warning, "Consent request {RequestId} will be retried")]
    internal static partial void RequestRetry(this ILogger<ConsentReconciler> logger, Guid requestId);

    [LoggerMessage(LogLevel.Warning, "Consent reconciliation store unavailable; activation remains closed")]
    internal static partial void StoreUnavailable(this ILogger<ConsentReconciler> logger);
}
