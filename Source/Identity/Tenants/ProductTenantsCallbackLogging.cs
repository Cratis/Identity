// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

internal static partial class ProductTenantsCallbackLogging
{
    [LoggerMessage(LogLevel.Warning, "Tenants callback for {Audience} unavailable for account {AccountId}: {Reason}")]
    internal static partial void Unavailable(this ILogger<ProductTenantsCallback> logger, string audience, string accountId, TenantsUnavailableReason reason);
}
