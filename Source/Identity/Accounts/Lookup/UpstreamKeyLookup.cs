// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>A keyed lookup pseudonym and its permanent account reservation.</summary>
/// <param name="Id">The indexed lookup digest.</param>
/// <param name="AccountSubject">The erasure subject and reserved owner.</param>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="Active">Whether authentication through this link is permitted.</param>
public record UpstreamKeyLookup([property: Index] UpstreamLookupDigest Id, [property: Subject] AccountSubject AccountSubject, LinkId LinkId, bool Active);
