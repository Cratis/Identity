// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.DPoP;

/// <summary>Non-sensitive proof rejection reasons.</summary>
internal enum DPoPFailure
{
    /// <summary>The proof is valid.</summary>
    None = 0,

    /// <summary>A required proof is absent.</summary>
    MissingProof = 1,

    /// <summary>The bounded compact proof is malformed.</summary>
    Malformed = 2,

    /// <summary>The algorithm is not ES256.</summary>
    UnsupportedAlgorithm = 3,

    /// <summary>The public key is invalid or contains forbidden material.</summary>
    InvalidJwk = 4,

    /// <summary>The signature is invalid.</summary>
    InvalidSignature = 5,

    /// <summary>The method is not exactly POST.</summary>
    HtmMismatch = 6,

    /// <summary>The target is not the exact configured endpoint.</summary>
    HtuMismatch = 7,

    /// <summary>The proof timestamp is outside the profile's window.</summary>
    IatOutsideWindow = 8,

    /// <summary>The proof identifier is missing or exceeds the bound.</summary>
    InvalidJti = 9,

    /// <summary>A fresh server nonce is required.</summary>
    NonceRequired = 10,

    /// <summary>The key does not match the refresh family.</summary>
    JktMismatch = 11,

    /// <summary>An unbound family cannot acquire a key on refresh.</summary>
    UnboundFamily = 12,

    /// <summary>The shared store already reserved this proof.</summary>
    ReusedJti = 13,

    /// <summary>The shared replay store failed.</summary>
    JtiCacheUnavailable = 14
}
