// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Keyed Chronicle custody binding and monotonic recovery facts.</summary>
internal interface IRecoveryFactWriter
{
    /// <summary>Establishes immutable custody binding.</summary>
    /// <param name="instanceId">Independent log identity.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Binding completion.</returns>
    Task Bind(string instanceId, CancellationToken cancellationToken);

    /// <summary>Publishes the original durable recovery intent.</summary>
    /// <param name="intent">The independent intent.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Publication completion.</returns>
    Task Publish(RecoveryEpochIntent intent, CancellationToken cancellationToken);

    /// <summary>Records a completely published contiguous prefix.</summary>
    /// <param name="instanceId">Independent log identity.</param>
    /// <param name="publishedThrough">The complete prefix.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Checkpoint completion.</returns>
    Task Checkpoint(string instanceId, long publishedThrough, CancellationToken cancellationToken);
}
