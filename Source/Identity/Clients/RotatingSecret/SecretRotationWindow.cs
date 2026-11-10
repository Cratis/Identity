// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.RotatingSecret;

/// <summary>The activation and overlap boundaries of a secret revision.</summary>
/// <param name="ActivatedAt">The replacement activation instant.</param>
/// <param name="PreviousValidUntil">The old revision deadline; equality means no overlap.</param>
internal sealed record SecretRotationWindow(DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil)
{
    /// <summary>Requires ordered deadlines and monotonic activation.</summary>
    /// <param name="previousActivation">The current revision activation time.</param>
    /// <returns>Whether both timeline invariants hold.</returns>
    public bool IsValidAfter(DateTimeOffset previousActivation) => PreviousValidUntil >= ActivatedAt && ActivatedAt >= previousActivation;
}
