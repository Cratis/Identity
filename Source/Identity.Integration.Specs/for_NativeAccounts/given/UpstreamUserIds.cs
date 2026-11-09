// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_NativeAccounts.given;

/// <summary>
/// Allocates a distinct upstream user id for every scenario initialization, because upstream key reservations are permanent
/// and every fact of a scenario shares one host and event store.
/// </summary>
static class UpstreamUserIds
{
    static long _next = 987_000_000;

    /// <summary>Gets the next unused upstream user id.</summary>
    /// <returns>The id.</returns>
    public static long Next() => Interlocked.Increment(ref _next);
}
