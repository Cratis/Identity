// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Defines the source of an external issuer's signing keys.
/// </summary>
internal interface IExternalIssuerKeys
{
    /// <summary>
    /// Gets the signing keys of a configured issuer. Keys are only ever fetched from the issuer's configured JWKS URI.
    /// </summary>
    /// <param name="issuer">The configured <see cref="ExternalIssuerOptions"/>.</param>
    /// <param name="keyId">The <c language="text">kid</c> the token names, if any; an unknown one may trigger a rate-limited refresh.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The keys, or <see langword="null"/> when none can be trusted (fetch failed and no usable cache).</returns>
    Task<IReadOnlyCollection<SecurityKey>?> Get(ExternalIssuerOptions issuer, string? keyId, CancellationToken cancellationToken = default);
}
