// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Clients.Events;

/// <summary>Internal secret activation metadata; carries only a producer-computed keyed digest.</summary>
/// <param name="ClientSecretId">The non-redeemable secret revision reference.</param>
/// <param name="ClientSecretHash">The purpose-separated, key-revision-prefixed HMAC-SHA-256 digest.</param>
/// <param name="ActivatedAt">The activation time.</param>
/// <param name="PreviousValidUntil">The bounded overlap end; equal to ActivatedAt when there is no overlap.</param>
[EventType]
public record ClientSecretRotated(ClientSecretId ClientSecretId, SecretDigest ClientSecretHash, DateTimeOffset ActivatedAt, DateTimeOffset PreviousValidUntil)
{
    readonly SecretDigest _clientSecretHash = ValidateDigest(ClientSecretHash);
    readonly DateTimeOffset _previousValidUntil = ValidateDeadline(ActivatedAt, PreviousValidUntil);

    /// <summary>
    /// Gets or initializes the digest, rejecting inherited concept-initializer bypasses before persistence.
    /// </summary>
    public SecretDigest ClientSecretHash
    {
        get => ValidateDigest(_clientSecretHash);
        init => _clientSecretHash = ValidateDigest(value);
    }

    /// <summary>
    /// Gets or initializes the overlap deadline, which cannot precede activation.
    /// </summary>
    public DateTimeOffset PreviousValidUntil
    {
        get => ValidateDeadline(ActivatedAt, _previousValidUntil);
        init => _previousValidUntil = ValidateDeadline(ActivatedAt, value);
    }

    /// <inheritdoc/>
    public override string ToString() => "ClientSecretRotated { ClientSecretHash = [redacted] }";

    static SecretDigest ValidateDigest(SecretDigest digest)
    {
        digest.EnsureValid();
        return digest;
    }

    static DateTimeOffset ValidateDeadline(DateTimeOffset activatedAt, DateTimeOffset previousValidUntil) =>
        previousValidUntil >= activatedAt ? previousValidUntil : throw new ArgumentOutOfRangeException(nameof(previousValidUntil), "The overlap deadline cannot precede activation.");
}
