// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Device;

/// <summary>Primary operational authority for rate windows across replicas.</summary>
internal interface IDeviceGrantStateStore
{
    /// <summary>Atomically increments a counter, starting a new window after logical expiry.</summary>
    /// <param name="id">The purpose-separated digest.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="window">The fixed window duration.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The count after this increment.</returns>
    Task<int> Increment(string id, DateTime now, TimeSpan window, CancellationToken cancellationToken);

    /// <summary>Reads one window.</summary>
    /// <param name="id">The keyed digest.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The persisted window, if present.</returns>
    Task<DeviceGrantState?> Read(string id, CancellationToken cancellationToken);

    /// <summary>Inserts or replaces only the observed revision.</summary>
    /// <param name="state">The successor.</param>
    /// <param name="revision">The observed revision, or null for an insert.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Whether this request won the CAS.</returns>
    Task<bool> Replace(DeviceGrantState state, long? revision, CancellationToken cancellationToken);
}
