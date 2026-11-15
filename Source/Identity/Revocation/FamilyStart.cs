// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// The initial admission of a refresh family.
/// </summary>
/// <param name="GrantId">The authorization.</param>
/// <param name="TokenId">The initial entry.</param>
/// <param name="Subject">The account.</param>
/// <param name="StartedAt">The admission time.</param>
/// <param name="AbsoluteExpiresAt">The original absolute expiry.</param>
/// <param name="Link">The account link the grant was authenticated through, or null when unknown.</param>
internal record FamilyStart(string GrantId, string TokenId, string Subject, DateTimeOffset StartedAt, DateTimeOffset AbsoluteExpiresAt, Guid? Link = null);
