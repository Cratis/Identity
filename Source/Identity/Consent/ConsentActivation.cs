// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow;

/// <summary>The operational consent activation barrier; not wired to the live authorization endpoint.</summary>
/// <param name="store">The authoritative operational store.</param>
/// <param name="evidence">The evidence append boundary.</param>
/// <param name="fence">The live revocation and session check.</param>
/// <param name="authorizations">The dormant authorization adapter.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="options">The validated deadlines.</param>
internal sealed class ConsentActivation(
    IConsentRequestStore store,
    IConsentEvidence evidence,
    IRevocationFence fence,
    IAuthorizationActivator authorizations,
    TimeProvider timeProvider,
    IOptions<ConsentActivationOptions> options)
{
    /// <summary>Creates a request from already validated server-side authorization parameters.</summary>
    /// <param name="binding">The immutable binding.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pending request.</returns>
    /// <exception cref="InvalidOperationException">The generated request identity already exists.</exception>
    public async Task<ConsentRequest> Create(ConsentBinding binding, CancellationToken cancellationToken)
    {
        var request = new ConsentRequest(ConsentRequestId.New(), binding, timeProvider.GetUtcNow() + options.Value.ActivationDeadline);
        if (!await store.Create(request, cancellationToken))
        {
            throw new InvalidOperationException("The consent request identity already exists.");
        }

        return request;
    }

    /// <summary>Durably decides a request after revalidating the server-side binding.</summary>
    /// <param name="id">The request identity.</param>
    /// <param name="binding">The authenticated binding, not submitted form values.</param>
    /// <param name="approve">Whether consent was granted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resulting snapshot, or null for an unknown or mismatched request.</returns>
    public async Task<ConsentRequest?> Decide(ConsentRequestId id, ConsentBinding binding, bool approve, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var request = await store.Read(id, cancellationToken);
            if (request is null || request.Binding != binding) return null;
            if (request.State != ConsentRequestState.Pending) return request;
            var state = DecisionState(request, approve);
            if (await Move(request, state, cancellationToken)) return await store.Read(id, cancellationToken);
        }

        return null;
    }

    /// <summary>Resumes an unresolved request; never issues a code.</summary>
    /// <param name="id">The request identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest snapshot.</returns>
    public async Task<ConsentRequest?> Resume(ConsentRequestId id, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var request = await store.Read(id, cancellationToken);
            if (request is null) return null;
            if (!await Advance(request, cancellationToken)) return await store.Read(id, cancellationToken);
        }

        return await store.Read(id, cancellationToken);
    }

    /// <summary>Consumes the single permission to issue a code, only for a live Active binding.</summary>
    /// <param name="id">The request identity.</param>
    /// <param name="binding">The authenticated binding.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization identity, or null. A crash after consumption intentionally requires a new request.</returns>
    public async Task<string?> ClaimCode(ConsentRequestId id, ConsentBinding binding, CancellationToken cancellationToken)
    {
        var request = await store.Read(id, cancellationToken);
        if (!CanClaim(request, binding)) return null;
        if (!await Eligible(request, cancellationToken)) return null;

        var claimed = request with { CodeClaimed = true, Revision = request.Revision + 1 };
        if (!await store.CompareExchange(request, claimed, timeProvider.GetUtcNow(), cancellationToken)) return null;
        return await Eligible(claimed, cancellationToken) ? claimed.AuthorizationId : null;
    }

    static bool CanClaim([NotNullWhen(true)] ConsentRequest? request, ConsentBinding binding) =>
        request is { State: ConsentRequestState.Active, CodeClaimed: false, AuthorizationId: not null } && request.Binding == binding;

    ConsentRequestState DecisionState(ConsentRequest request, bool approve) => timeProvider.GetUtcNow() >= request.Deadline
        ? ConsentRequestState.Abandoned
        : approve ? ConsentRequestState.Decided : ConsentRequestState.Denied;

    Task<bool> Advance(ConsentRequest request, CancellationToken cancellationToken) => request.State switch
    {
        ConsentRequestState.Pending => ExpirePending(request, cancellationToken),
        ConsentRequestState.Decided => RecordOrResolve(request, cancellationToken),
        ConsentRequestState.EvidenceRecorded => Activate(request, cancellationToken),
        _ => Task.FromResult(false)
    };

    async Task<bool> ExpirePending(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow() < request.Deadline) return false;
        await Move(request, ConsentRequestState.Abandoned, cancellationToken);
        return true;
    }

    async Task<bool> RecordOrResolve(ConsentRequest request, CancellationToken cancellationToken)
    {
        ConsentEvidenceResolution resolution;
        try
        {
            resolution = await (await Eligible(request, cancellationToken)
                ? evidence.Record(request, cancellationToken)
                : evidence.Resolve(request.Id, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false; // A failed acknowledgement is not evidence of absence.
        }

        if (resolution is not (ConsentEvidenceResolution.Recorded or ConsentEvidenceResolution.NotRecorded)) return false;
        var next = resolution == ConsentEvidenceResolution.Recorded ? ConsentRequestState.EvidenceRecorded : ConsentRequestState.Abandoned;
        await Move(request, next, cancellationToken);
        return true;
    }

    async Task<bool> Activate(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (!await Eligible(request, cancellationToken))
        {
            return await Move(request, ConsentRequestState.EvidenceWithheld, cancellationToken);
        }

        var authorizationId = await authorizations.Prepare(request, cancellationToken);
        if (authorizationId is null) return false;
        if (!await Eligible(request, cancellationToken))
        {
            return await Move(request, ConsentRequestState.EvidenceWithheld, cancellationToken);
        }

        var active = request with
        {
            State = ConsentRequestState.Active,
            AuthorizationId = authorizationId,
            Revision = request.Revision + 1
        };
        await store.CompareExchange(request, active, timeProvider.GetUtcNow(), cancellationToken);
        return true;
    }

    async Task<bool> Eligible(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow() >= request.Deadline) return false;
        try
        {
            return await fence.IsClear(request, cancellationToken) && timeProvider.GetUtcNow() < request.Deadline;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    Task<bool> Move(ConsentRequest request, ConsentRequestState state, CancellationToken cancellationToken)
    {
        var replacement = request with
        {
            State = state,
            Revision = request.Revision + 1,
            PurgeAt = state is ConsentRequestState.Denied or ConsentRequestState.Abandoned or ConsentRequestState.EvidenceWithheld
                ? timeProvider.GetUtcNow() + options.Value.TerminalRetention : null
        };
        return store.CompareExchange(request, replacement, timeProvider.GetUtcNow(), cancellationToken);
    }
}
