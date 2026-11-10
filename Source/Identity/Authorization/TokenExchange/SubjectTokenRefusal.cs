// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Defines why a subject token was refused. The category is for server logs only; callers see one identical error.
/// </summary>
internal enum SubjectTokenRefusal
{
    /// <summary>
    /// The subject token was accepted.
    /// </summary>
    None = 0,

    /// <summary>
    /// The token is oversized, is not a compact JWS, or cannot be parsed.
    /// </summary>
    Malformed = 1,

    /// <summary>
    /// The token names an issuer that is not configured.
    /// </summary>
    UnknownIssuer = 2,

    /// <summary>
    /// The issuer is configured but the client may not present its tokens.
    /// </summary>
    IssuerNotAllowed = 3,

    /// <summary>
    /// Signature, algorithm, type, audience, issuer or lifetime validation failed.
    /// </summary>
    Invalid = 4,

    /// <summary>
    /// A required claim (<c language="text">sub</c>, <c language="text">iat</c>) is missing or hostile.
    /// </summary>
    MissingClaim = 5,

    /// <summary>
    /// The token was issued too long ago or in the future.
    /// </summary>
    Stale = 6,

    /// <summary>
    /// The token has been presented before.
    /// </summary>
    Replayed = 7,

    /// <summary>
    /// The issuer's keys could not be obtained; nothing is issued, and the caller may retry.
    /// </summary>
    KeysUnavailable = 8,

    /// <summary>
    /// The replay guard is full, so the token cannot be recorded; nothing is issued, and the caller may retry.
    /// </summary>
    ReplayGuardFull = 9,

    /// <summary>
    /// The replay store is unavailable; nothing is issued, and the caller may retry.
    /// </summary>
    ReplayGuardUnavailable = 10
}
