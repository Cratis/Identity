// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The guarded account decision state, never an HTTP query.</summary>
/// <param name="Id">The account subject.</param>
[Passive]
internal record AccountLinkState(string Id)
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

    /// <summary>
    /// Refuses missing, disabled, or terminal accounts.
    /// </summary>
    /// <param name="state">The guarded account state.</param>
    /// <returns>A refusal, or null when the account is active.</returns>
    internal static ValidationResult? RequireActive(AccountLinkState? state) => state is { Registered: true, Tombstoned: false, Disabled: false }
        ? null
        : ValidationResult.Error("account_not_active");

    /// <summary>
    /// Finds a proposal belonging to the guarded account.
    /// </summary>
    /// <param name="state">The guarded account state.</param>
    /// <param name="id">The proposal identifier.</param>
    /// <returns>The proposal, or a not-found refusal.</returns>
    internal static Result<LinkProposal, ValidationResult> FindProposal(AccountLinkState state, LinkProposalId id) =>
        state.Proposals.TryGetValue(id.Value, out var proposal) ? proposal : ValidationResult.Error("proposal_not_found");
}
