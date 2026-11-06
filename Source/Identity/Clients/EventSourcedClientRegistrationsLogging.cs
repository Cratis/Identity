// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

internal static partial class EventSourcedClientRegistrationsLogging
{
    [LoggerMessage(LogLevel.Information, "Chronicle client registrations reconciled with {Count} declared clients")]
    internal static partial void EventSourcedClientsReconciled(this ILogger<EventSourcedClientRegistrations> logger, int count);
}
