// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients;

/// <summary>Internal client decision state, reconstructed only from Internal facts.</summary>
/// <param name="Id">The opaque stream identity, unrelated to the protocol client id.</param>
internal sealed record ClientRegistration(Guid Id)
{
    /// <summary>Gets the protocol identifier, if registered.</summary>
    public ClientId? ClientId { get; init; }

    /// <summary>Gets accepted configuration, never plaintext credentials.</summary>
    public ClientConfiguration Configuration { get; init; } = new();

    /// <summary>Gets the current secret revision.</summary>
    public ClientSecretId? SecretId { get; init; }

    /// <summary>Gets the current keyed digest.</summary>
    public SecretDigest? SecretHash { get; init; }

    /// <summary>Gets the previous keyed digest during an overlap window.</summary>
    public SecretDigest? PreviousSecretHash { get; init; }

    /// <summary>Gets the current activation time.</summary>
    public DateTimeOffset ActivatedAt { get; init; }

    /// <summary>Gets the previous secret deadline; equality means no overlap.</summary>
    public DateTimeOffset PreviousValidUntil { get; init; }

    /// <summary>Gets whether retirement permanently ended this registration.</summary>
    public bool Retired { get; init; }

    /// <summary>Gets the recorded retirement time.</summary>
    public DateTimeOffset? RetiredAt { get; init; }
}
