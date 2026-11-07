// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Immutable denial identity; retries and log recovery preserve every field.</summary>
/// <param name="Id">The permanent fence identity.</param>
/// <param name="Sequence">The authority-partition sequence, distinct from Chronicle positions.</param>
/// <param name="GrantId">The operational authorization identifier.</param>
/// <param name="Subject">The account subject.</param>
/// <param name="Reason">The closed restriction reason.</param>
/// <param name="RecordedAt">The restriction time.</param>
internal sealed record GrantFence(Guid Id, long Sequence, string GrantId, string Subject, RevocationReason Reason, DateTimeOffset RecordedAt);
