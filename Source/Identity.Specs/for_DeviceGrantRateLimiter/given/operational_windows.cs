// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;

namespace Cratis.Identity.for_DeviceGrantRateLimiter.given;

internal sealed class operational_windows : IDeviceGrantStateStore
{
    readonly object _lock = new();
    internal readonly Dictionary<string, DeviceGrantState> Rows = [];
    internal bool Unavailable;
    internal bool Contended;
    internal int Writes;

    public Task<int> Increment(string id, DateTime now, TimeSpan window, CancellationToken cancellationToken)
    {
        if (Unavailable) throw new InvalidOperationException("unavailable");
        lock (_lock)
        {
            Writes++;
            var previous = Rows.GetValueOrDefault(id);
            var current = previous is not null && previous.ExpiresAt > now ? previous : null;
            var next = new DeviceGrantState { Id = id, Attempts = (current?.Attempts ?? 0) + 1, ExpiresAt = current?.ExpiresAt ?? now.Add(window) };
            Rows[id] = next;
            return Task.FromResult(next.Attempts);
        }
    }

    public async Task<DeviceGrantState?> Read(string id, CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (Unavailable) throw new InvalidOperationException("unavailable");
        lock (_lock) return Rows.GetValueOrDefault(id);
    }

    public async Task<bool> Replace(DeviceGrantState state, long? revision, CancellationToken cancellationToken)
    {
        await Task.Yield();
        lock (_lock)
        {
            Writes++;
            if (Contended || Rows.GetValueOrDefault(state.Id)?.Revision != revision) return false;
            Rows[state.Id] = state;
            return true;
        }
    }
}
