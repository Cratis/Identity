// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// The outcome of recording a subject token in the <see cref="SubjectTokenReplayGuard"/>.
/// </summary>
internal enum ReplayRecordResult
{
    /// <summary>The token was recorded now.</summary>
    Recorded = 0,

    /// <summary>The token was used before.</summary>
    AlreadyUsed = 1,

    /// <summary>The guard is full, so nothing more is accepted.</summary>
    Full = 2,

    /// <summary>
    /// The guard could not durably record the token.
    /// </summary>
    Unavailable = 3
}
