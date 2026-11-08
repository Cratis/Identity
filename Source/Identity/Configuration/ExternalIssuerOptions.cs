// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Represents an external token issuer whose subject tokens Identity accepts for RFC 8693 token exchange.
/// </summary>
public class ExternalIssuerOptions
{
    /// <summary>
    /// Gets or sets the exact <c language="text">iss</c> value of the issuer. It must be an absolute HTTPS URI and is compared ordinally.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute URI of the issuer's JWKS document. HTTPS is required outside Development.
    /// </summary>
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accepted <c language="text">aud</c> values of a subject token. At least one is required.
    /// </summary>
    public IList<string> Audiences { get; set; } = [];

    /// <summary>
    /// Gets or sets the accepted signing algorithms. Defaults to RS256 and ES256; <c language="text">none</c> and HMAC algorithms are refused.
    /// </summary>
    public IList<string> SigningAlgorithms { get; set; } = ["RS256", "ES256"];

    /// <summary>
    /// Gets or sets the clock skew tolerated for lifetime checks. Defaults to 60 seconds; at most 120 seconds.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the longest age of a subject token, measured from <c language="text">iat</c>. Defaults to ten minutes; at most one hour.
    /// </summary>
    public TimeSpan MaxTokenAge { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the deadline for fetching the JWKS document. Defaults to three seconds; at most ten seconds.
    /// </summary>
    public TimeSpan JwksTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
