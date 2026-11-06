// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Owner-scoped lookup reservations. Child keys are opaque LinkIds, never PII digests.</summary>
/// <param name="Id">The account subject and erasure boundary.</param>
/// <param name="Keys">The permanent reservations, including inactive keys; each reservation's KeyHash is indexed.</param>
public record UpstreamKeyLookup(AccountSubject Id, IEnumerable<UpstreamKeyReservation> Keys);
