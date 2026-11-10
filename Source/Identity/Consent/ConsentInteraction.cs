// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Pending;
using Cratis.Identity.Realm;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow;

/// <summary>The package-owned query/decision boundary. Never writes evidence, activates grants or issues codes.</summary>
/// <param name="store">The operational CAS store.</param>
/// <param name="caller">The trusted caller binding.</param>
/// <param name="eligibility">The passive catalogue revalidation.</param>
/// <param name="proofs">The protected session-bound proofs.</param>
/// <param name="time">The server clock.</param>
/// <param name="options">The terminal retention policy.</param>
internal sealed class ConsentInteraction(IConsentRequestStore store, ConsentCaller caller, IConsentEligibility eligibility, IConsentProof proofs, TimeProvider time, IOptions<ConsentActivationOptions> options)
{
    /// <summary>Finds the newest eligible pending request without changing state.</summary>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The escaped package display or null.</returns>
    internal async Task<PendingConsent?> Find(CancellationToken cancellationToken)
    {
        using var realm = IdentityRealmScope.Begin();
        if (caller.Resolve() is not { } owner) return null;
        var request = await store.FindPending(owner.Account, owner.Session, owner.Realm, time.GetUtcNow(), cancellationToken);
        if (!IsPending(request, owner)) return null;
        var eligible = await eligibility.Evaluate(request!, cancellationToken);
        return eligible is null || !IsPending(request, owner) ? null : ConsentPresentation.Create(request!, eligible, proofs.Issue(request!));
    }

    /// <summary>Performs a single pending-to-terminal-decision CAS after complete revalidation.</summary>
    /// <param name="id">The submitted request identity.</param>
    /// <param name="tenant">The selected tenant for approval.</param>
    /// <param name="scopes">The selected scope set for approval.</param>
    /// <param name="proof">The package proof.</param>
    /// <param name="approve">Whether this is an approval.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="ConsentDecisionRefused">Any request, caller, proof, eligibility or CAS check fails.</exception>
    internal async Task Decide(ConsentRequestId id, string? tenant, string[]? scopes, string? proof, bool approve, CancellationToken cancellationToken)
    {
        using var realm = IdentityRealmScope.Begin();
        var owner = caller.Resolve() ?? throw new ConsentDecisionRefused();
        var request = await store.Read(id, cancellationToken);
        if (!IsPending(request, owner) || !proofs.Validate(ConsentProofToken.From(proof), request!)) throw new ConsentDecisionRefused();
        var eligible = await eligibility.Evaluate(request!, cancellationToken) ?? throw new ConsentDecisionRefused();
        var selection = approve ? Select(request!, eligible, tenant, scopes) : null;
        if (caller.Resolve() != owner || !IsPending(request, owner)) throw new ConsentDecisionRefused();
        var replacement = request! with
        {
            State = approve ? ConsentRequestState.Decided : ConsentRequestState.Denied,
            Selection = selection,
            Revision = request.Revision + 1,
            PurgeAt = approve ? null : time.GetUtcNow() + options.Value.TerminalRetention
        };
        if (!await store.CompareExchange(request, replacement, time.GetUtcNow(), cancellationToken)) throw new ConsentDecisionRefused();
    }

    static ConsentSelection Select(ConsentRequest request, ConsentEligibility eligible, string? tenant, string[]? scopes)
    {
        if (tenant is null || !eligible.Tenants.Any(_ => _.Id == tenant) || scopes?.Any(string.IsNullOrWhiteSpace) != false) throw new ConsentDecisionRefused();
        var selected = scopes.ToHashSet(StringComparer.Ordinal);
        var requested = request.Binding.Scopes.Scopes.Select(_ => _.Value);
        if (selected.Count != scopes.Length || !selected.SetEquals(requested)) throw new ConsentDecisionRefused();
        return new(tenant, ScopeSet.From(selected.Select(_ => new Scope(_))));
    }

    bool IsPending(ConsentRequest? request, ConsentCallerIdentity owner) => request is { State: ConsentRequestState.Pending } &&
        request.Binding.Account == owner.Account && request.Binding.Session == owner.Session && request.Binding.Realm == owner.Realm && time.GetUtcNow() < request.Deadline;
}
