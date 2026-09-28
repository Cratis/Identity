// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>
/// Holds log messages for <see cref="ClientRegistrations"/>.
/// </summary>
internal static partial class ClientRegistrationsLogging
{
    [LoggerMessage(LogLevel.Warning, "Removing client {ClientId}, which is no longer declared")]
    internal static partial void RemovingUndeclaredClient(this ILogger<ClientRegistrations> logger, string? clientId);

    [LoggerMessage(LogLevel.Information, "Client store reconciled with {Count} declared clients")]
    internal static partial void ClientsReconciled(this ILogger<ClientRegistrations> logger, int count);
}
