// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Registration;

/// <summary>Passive, fact-only client state for guarded startup decisions.</summary>
/// <param name="Id">The opaque stream identity.</param>
[Passive]
public record ClientRegistrationState(string Id)
{
    /// <summary>Gets the stream identity as a reviewed concept.</summary>
    [NotProjected]
    public ClientRegistrationId ClientRegistrationId => new(Guid.Parse(Id));

    /// <summary>Gets the OAuth identifier, or null before registration.</summary>
    public ClientId? ClientId { get; init; }

    /// <summary>Gets the registration origin.</summary>
    public RegistrationKind RegistrationKind { get; init; }

    /// <summary>Gets the accepted redirect set.</summary>
    public IEnumerable<RedirectUri> RedirectUris { get; init; } = [];

    /// <summary>Gets the current secret reference.</summary>
    public OpaqueReferenceId? CurrentSecretId { get; init; }

    /// <summary>Gets the approved keyed comparand, never the secret.</summary>
    public SecretDigest? CurrentSecretHash { get; init; }

    /// <summary>Gets the current secret activation time.</summary>
    public DateTimeOffset? SecretActivatedAt { get; init; }

    /// <summary>Gets whether the registration is retired.</summary>
    public bool Retired { get; init; }
}
