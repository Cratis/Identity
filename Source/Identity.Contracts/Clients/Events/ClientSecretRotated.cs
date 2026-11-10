// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal secret activation metadata; carries only a producer-computed keyed digest.</summary>
/// <param name="ClientSecretId">The non-redeemable secret revision reference.</param>
/// <param name="ClientSecretHash">The purpose-separated, key-revision-prefixed HMAC-SHA-256 digest.</param>
/// <param name="ActivatedAt">The activation time.</param>
/// <param name="PreviousValidUntil">The bounded overlap end; equal to ActivatedAt when there is no overlap.</param>
[EventType("4a913d36-056b-4e71-81ca-c90b207393b3", generation: 1)]
public record ClientSecretRotated(ClientSecretId ClientSecretId, SecretDigest ClientSecretHash, DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil)
{
    /// <inheritdoc/>
    public override string ToString() => "ClientSecretRotated { ClientSecretHash = [redacted] }";
}
