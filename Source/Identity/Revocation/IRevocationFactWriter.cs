// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Keyed source-fact append boundary, independent of delivery marking.</summary>
internal interface IRevocationFactWriter
{
    /// <summary>Returns only after a successful or already-committed keyed append.</summary>
    /// <param name="entry">The immutable intent.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable append.</returns>
    Task Append(OutboxEntry entry, CancellationToken cancellationToken);
}
