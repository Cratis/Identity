// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>An opaque child row inside its account's erasure boundary.</summary>
/// <param name="Id">The non-PII LinkId used for routing.</param>
/// <param name="KeyHash">The indexed, protected lookup pseudonym; never a routing key.</param>
/// <param name="Active">Whether the link is still active.</param>
internal record UpstreamKeyReservation(LinkId Id, [property: Index] UpstreamLookupDigest KeyHash, bool Active);
