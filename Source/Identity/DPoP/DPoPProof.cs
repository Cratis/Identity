// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.DPoP;

/// <summary>The verified, bounded proof fields; never logged.</summary>
/// <param name="Jkt">The RFC 7638 public key thumbprint.</param>
/// <param name="Jti">The proof identifier.</param>
/// <param name="Nonce">The server nonce, if supplied.</param>
internal sealed record DPoPProof(string Jkt, string Jti, string? Nonce);
