// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal secret-rotation metadata; the keyed digest never contains the secret.</summary>
/// <param name="ClientSecretId">A generated non-redeemable UUID reference.</param>
/// <param name="ClientSecretHash">A domain-separated HMAC produced with the separate client-secret custody key.</param>
/// <param name="ActivatedAt">The activation time.</param>
/// <param name="PreviousValidUntil">The previous-secret deadline; configuration rotations have no overlap.</param>
[EventType("b9be9c74-a329-4528-971a-83eaffdb26e3", generation: 1)]
public record ClientSecretRotated(OpaqueReferenceId ClientSecretId, SecretDigest ClientSecretHash, DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil);
