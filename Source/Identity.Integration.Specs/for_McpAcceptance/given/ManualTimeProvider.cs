// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// A <see cref="TimeProvider"/> that only moves when a spec advances it, so cache windows are proven without waiting.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    DateTimeOffset _now = DateTimeOffset.UtcNow;

    /// <summary>
    /// Moves the clock forward.
    /// </summary>
    /// <param name="by">How far to move it.</param>
    public void Advance(TimeSpan by) => _now += by;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;
}
