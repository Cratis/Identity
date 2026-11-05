// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.given;

/// <summary>
/// A certificate and its private key, both PEM encoded.
/// </summary>
/// <param name="Certificate">The certificate PEM.</param>
/// <param name="Key">The private key PEM.</param>
public record PemPair(string Certificate, string Key);
