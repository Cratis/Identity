// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Clients;

/// <summary>
/// A v&lt;key revision&gt;.&lt;base64url HMAC-SHA-256&gt; digest produced before append.
/// The producer must use a separate secret-store key and the purpose
/// cratis.identity.client-secret.v1. Syntax validation cannot establish keyed provenance.
/// </summary>
public sealed partial record SecretDigest : ConceptAs<string>, IJsonOnSerializing
{
    /// <summary>Wraps a producer-computed digest, never plaintext or a reversible envelope.</summary>
    /// <param name="value">The revision-prefixed, unpadded canonical digest.</param>
    public SecretDigest(string value) : base(value)
    {
        EnsureValid();
    }

    [GeneratedRegex(@"\Av[1-9][0-9]*\.[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DigestFormat { get; }

    /// <summary>Checks only the closed wire format, not the producer's keyed construction.</summary>
    /// <param name="value">The proposed digest.</param>
    /// <returns>Whether the digest has the required format.</returns>
    public static bool IsWellFormed(string? value) => value is not null && DigestFormat.IsMatch(value);

    /// <inheritdoc/>
    void IJsonOnSerializing.OnSerializing() => EnsureValid();

    /// <summary>
    /// Rejects inherited initializer bypasses at an event or serializer boundary.
    /// </summary>
    /// <exception cref="ArgumentException">The current value is not a digest.</exception>
    internal void EnsureValid() => _ = Validate(Value);

    static string Validate(string value) => IsWellFormed(value) ? value : throw new ArgumentException("Invalid secret digest.", nameof(value));
}
