// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.RotatingSecret;

/// <summary>The activation and overlap boundaries of a secret revision.</summary>
/// <param name="ActivatedAt">The replacement activation instant.</param>
/// <param name="PreviousValidUntil">The old revision deadline; equality means no overlap.</param>
internal sealed record SecretRotationWindow(DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil)
{
    /// <summary>Requires bounded activation, monotonic revisions, and a capped overlap.</summary>
    /// <param name="previousActivation">The current revision activation time.</param>
    /// <param name="now">The trusted server time.</param>
    /// <param name="maxOverlap">The configured maximum overlap.</param>
    /// <returns>Whether all timeline invariants hold.</returns>
    public bool IsValidAfter(DateTimeOffset previousActivation, DateTimeOffset now, TimeSpan maxOverlap)
    {
        if (ActivatedAt == DateTimeOffset.MaxValue || PreviousValidUntil == DateTimeOffset.MaxValue)
        {
            return false;
        }

        return IsOrderedAfter(previousActivation) && IsNear(now) && HasBoundedOverlap(maxOverlap);
    }

    bool IsOrderedAfter(DateTimeOffset previousActivation) => PreviousValidUntil >= ActivatedAt && ActivatedAt >= previousActivation;

    bool IsNear(DateTimeOffset now) => (ActivatedAt - now).Duration() <= TimeSpan.FromDays(1);

    bool HasBoundedOverlap(TimeSpan maxOverlap) => maxOverlap >= TimeSpan.Zero && PreviousValidUntil - ActivatedAt <= maxOverlap;
}
