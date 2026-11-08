// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Closed, non-personal reasons for a permanent grant fence.</summary>
internal enum RevocationReason
{
    /// <summary>Explicit grant revocation.</summary>
    Revoked = 0,

    /// <summary>A refresh token was presented more than once.</summary>
    RefreshTokenReused = 1,

    /// <summary>RFC 7009 refresh-token revocation.</summary>
    RefreshTokenRevoked = 2,

    /// <summary>Reserved for consent withdrawal.</summary>
    ConsentWithdrawn = 3
}
