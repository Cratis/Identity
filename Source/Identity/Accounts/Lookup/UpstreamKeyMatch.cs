// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>A reservation result, not a proof of permission to authenticate.</summary>
/// <param name="AccountSubject">The reserved account owner.</param>
/// <param name="LinkId">The opaque key reference.</param>
/// <param name="Active">Whether the reservation remains active.</param>
public record UpstreamKeyMatch(AccountSubject AccountSubject, LinkId LinkId, bool Active);
