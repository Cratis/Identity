// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.Lookup;

namespace Cratis.Identity.Upstream;

/// <summary>Checks lookup provenance before a decision read can enroll an immutable key.</summary>
/// <param name="keys">The configured lookup key.</param>
/// <param name="digester">The keyed digester.</param>
public sealed class VerifiedKeyGate(IUpstreamLookupKeys keys, UpstreamKeyDigester digester)
{
    /// <summary>Computes lookup metadata at the verified protocol boundary.</summary>
    /// <param name="authentication">The adapter-verified authentication.</param>
    /// <returns>The lookup metadata.</returns>
    public VerifiedSignInKey KeyFor(VerifiedAuthentication authentication) => new(
        authentication.ConnectionId,
        authentication.IdpIssuer,
        authentication.Subject,
        keys.Current.Revision,
        digester.Digest(authentication.IdpIssuer, authentication.Subject));

    /// <summary>Refuses mismatched connection, immutable pair, revision or digest.</summary>
    /// <param name="authentication">The adapter-verified authentication.</param>
    /// <param name="key">The lookup metadata selected for this operation.</param>
    /// <returns>A rejection, or null when the provenance matches.</returns>
    public ValidationResult? Validate(VerifiedAuthentication authentication, VerifiedSignInKey key) =>
        authentication.ConnectionId == GitHubConnection.FixedConnectionId &&
        authentication.IdpIssuer.Value == "https://github.com" && !string.IsNullOrWhiteSpace(authentication.Subject.Value) &&
        key.ConnectionId == authentication.ConnectionId && key.IdpIssuer == authentication.IdpIssuer && key.IdpSubject == authentication.Subject &&
        key.Revision == keys.Current.Revision && key.Digest == digester.Digest(authentication.IdpIssuer, authentication.Subject)
            ? null
            : ValidationResult.Error("verified_upstream_key_mismatch");
}
