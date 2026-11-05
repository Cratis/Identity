// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when a spec moves it.
/// </summary>
/// <param name="now">The initial time.</param>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
