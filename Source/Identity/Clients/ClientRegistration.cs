// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
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

    /// <summary>Gets the latest keyed digest, which may have a future activation.</summary>
    public SecretDigest? SecretHash { get; init; }

    /// <summary>Gets the previous keyed digest during an overlap window.</summary>
    public SecretDigest? PreviousSecretHash { get; init; }

    /// <summary>Gets retained revisions needed across successive scheduled rotations.</summary>
    public ClientSecretRevision[] PreviousSecretRevisions { get; init; } = [];

    /// <summary>Gets the latest revision's activation time.</summary>
    public DateTimeOffset ActivatedAt { get; init; }

    /// <summary>Gets the previous secret deadline; equality means no overlap.</summary>
    public DateTimeOffset PreviousValidUntil { get; init; }

    /// <summary>Gets whether retirement permanently ended this registration.</summary>
    public bool Retired { get; init; }

    /// <summary>Gets the recorded retirement time.</summary>
    public DateTimeOffset? RetiredAt { get; init; }

    /// <summary>Requires an existing, non-retired registration.</summary>
    /// <param name="state">The guarded state.</param>
    /// <returns>A credential-free refusal, or null.</returns>
    public static ValidationResult? RequireActive(ClientRegistration? state)
    {
        if (state?.ClientId is null)
        {
            return ValidationResult.Error("client_not_registered");
        }

        return state.Retired ? ValidationResult.Error("client_retired") : null;
    }

    /// <summary>Gets only digests valid at an explicit instant, never at reducer execution time.</summary>
    /// <param name="at">The verification instant.</param>
    /// <returns>The valid digest revisions, or none for a retired registration.</returns>
    public IEnumerable<SecretDigest> ValidSecretHashesAt(DateTimeOffset at)
    {
        if (Retired)
        {
            yield break;
        }

        foreach (var revision in PreviousSecretRevisions.Where(revision => revision.IsValidAt(at)))
        {
            yield return revision.Digest;
        }

        if (SecretHash is not null && at >= ActivatedAt)
        {
            yield return SecretHash;
        }
    }
}
