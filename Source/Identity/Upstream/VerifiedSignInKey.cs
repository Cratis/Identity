// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>The exact immutable pair and lookup revision supplied by the verified protocol boundary.</summary>
/// <param name="ConnectionId">The configured connection.</param>
/// <param name="IdpIssuer">The exact verified issuer.</param>
/// <param name="IdpSubject">The verified immutable subject.</param>
/// <param name="Revision">The current configured lookup revision.</param>
/// <param name="Digest">The keyed digest of the immutable pair.</param>
public record VerifiedSignInKey(ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamSubject IdpSubject, int Revision, UpstreamLookupDigest Digest)
{
    /// <inheritdoc/>
    public override string ToString() => "Verified upstream key [redacted]";
}
