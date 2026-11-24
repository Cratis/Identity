// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>
/// A confidential client's secret was set or rotated. This is the only secret-related fact consumers receive: the revision reference and
/// the dates. The secret, and the keyed digest Identity verifies it against, never leave Identity.
/// </summary>
/// <param name="ClientSecretId">The non-redeemable secret revision reference.</param>
/// <param name="ActivatedAt">The activation time.</param>
/// <param name="PreviousValidUntil">The bounded overlap end for the previous secret; equal to <paramref name="ActivatedAt"/> when there is no overlap.</param>
[EventType, Public]
public record SecretRotatedForClient(ClientSecretId ClientSecretId, DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil);
