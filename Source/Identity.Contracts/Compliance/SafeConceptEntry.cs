// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Compliance;

/// <summary>
/// Reviewed producer evidence for a non-redeemable event leaf; never payload metadata.
/// </summary>
/// <param name="UnderlyingType">The serialized primitive type.</param>
/// <param name="Kind">The exception category.</param>
/// <param name="ProducerSpec">The producing-writer specification.</param>
/// <param name="Justification">The reviewed reason for the exception.</param>
/// <param name="Algorithm">The digest algorithm.</param>
/// <param name="Keyed">Whether the digest uses a separate secret-store-held key.</param>
/// <param name="Redeemable">Whether possession permits redemption.</param>
/// <param name="Generated">Whether the reference is generated.</param>
/// <param name="Canonicalization">The public-key canonicalization.</param>
/// <param name="Purpose">The domain separation purpose.</param>
/// <param name="HashKeyRevision">The revision representation.</param>
/// <param name="InputEntropy">The reviewed input entropy.</param>
public sealed record SafeConceptEntry(
    string UnderlyingType,
    string Kind,
    string ProducerSpec,
    string Justification,
    string? Algorithm = null,
    bool Keyed = false,
    bool Redeemable = true,
    bool Generated = false,
    string? Canonicalization = null,
    string? Purpose = null,
    string? HashKeyRevision = null,
    string? InputEntropy = null);
