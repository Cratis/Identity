// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients;

/// <summary>A retained digest revision with a half-open validity interval.</summary>
/// <param name="Digest">The keyed digest, never plaintext.</param>
/// <param name="ActivatedAt">The inclusive activation instant.</param>
/// <param name="ValidUntil">The exclusive supersession deadline.</param>
internal sealed record ClientSecretRevision(SecretDigest Digest, DateTimeOffset ActivatedAt, DateTimeOffset ValidUntil)
{
    /// <summary>Checks the recorded validity window without using replay-time wall clocks.</summary>
    /// <param name="at">The verification instant.</param>
    /// <returns>Whether the revision is valid at that instant.</returns>
    public bool IsValidAt(DateTimeOffset at) => at >= ActivatedAt && at < ValidUntil;
}
