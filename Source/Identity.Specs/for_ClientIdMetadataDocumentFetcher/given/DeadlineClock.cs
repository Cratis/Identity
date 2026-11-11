// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ClientIdMetadataDocumentFetcher.given;

/// <summary>Fires the fetch deadline on demand without waiting for wall-clock time.</summary>
public sealed class DeadlineClock : TimeProvider
{
    DeadlineTimer? _timer;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _timer = new DeadlineTimer(callback, state);
        return _timer;
    }

    public void Expire() => _timer?.Fire();

    sealed class DeadlineTimer(TimerCallback callback, object? state) : ITimer
    {
        bool _disposed;

        public void Fire()
        {
            if (!_disposed) callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
