// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Protocol;

namespace Cratis.Identity.Integration.given;

/// <summary>Freezes one real projection's reported position only in the owning test's async flow.</summary>
public sealed class ProjectionProgressGate
{
    readonly AsyncLocal<Hold?> _current = new();

    /// <summary>Gets the signal that the protocol has reached the held progress fence.</summary>
    public Task Observed => _current.Value.Observed.Task;

    /// <summary>Holds a projection at a previously observed native position.</summary>
    /// <param name="model">The projection model.</param>
    /// <param name="position">Its actual native position before new input was appended.</param>
    public void Freeze(Type model, ObserverPosition position) => _current.Value = new(model, position);

    /// <summary>Releases the hold in every child task sharing this test's state.</summary>
    public void Release() => _current.Value?.Release();

    /// <summary>Returns the held position, or no override for unrelated tests and projections.</summary>
    /// <param name="model">The projection model.</param>
    /// <returns>The frozen position while held.</returns>
    public ObserverPosition? Position(Type model) => _current.Value?.PositionFor(model);

    sealed class Hold(Type model, ObserverPosition position)
    {
        volatile bool _released;

        public TaskCompletionSource Observed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _released = true;

        public ObserverPosition? PositionFor(Type candidate)
        {
            if (_released || candidate != model)
            {
                return null;
            }

            Observed.TrySetResult();
            return position;
        }
    }
}
