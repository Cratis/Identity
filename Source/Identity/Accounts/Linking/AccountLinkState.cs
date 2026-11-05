// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The guarded account decision state, never an HTTP query.</summary>
/// <param name="Id">The account subject.</param>
[Passive]
public record AccountLinkState(string Id)
{
    /// <summary>Gets whether the subject was allocated.</summary>
    public bool Registered { get; init; }

    /// <summary>Gets whether the subject is terminal.</summary>
    public bool Tombstoned { get; init; }

    /// <summary>Gets whether authentication is disabled.</summary>
    public bool Disabled { get; init; }

    /// <summary>Gets the latest profile email.</summary>
    public Email? Email { get; init; }

    /// <summary>Gets the latest profile name.</summary>
    public PersonalName? Name { get; init; }

    /// <summary>Gets all proposals, including terminal ones.</summary>
    [NotProjected]
    public IReadOnlyDictionary<Guid, LinkProposal> Proposals { get; init; } = new Dictionary<Guid, LinkProposal>();

    /// <summary>Gets active confirmed links.</summary>
    [NotProjected]
    public IReadOnlyDictionary<Guid, UpstreamKeyLinkRecorded> Links { get; init; } = new Dictionary<Guid, UpstreamKeyLinkRecorded>();

    /// <summary>Gets per-key display-only identifiers.</summary>
    [NotProjected]
    public IReadOnlyDictionary<Guid, Login> Logins { get; init; } = new Dictionary<Guid, Login>();
}
