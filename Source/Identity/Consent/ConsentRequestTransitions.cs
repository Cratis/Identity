// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Guards the operational transition graph independently of orchestrator callers.</summary>
internal static class ConsentRequestTransitions
{
    /// <summary>Checks immutable parameters, monotonic issuance, retention and the next state.</summary>
    /// <param name="before">The expected snapshot.</param>
    /// <param name="after">The proposed replacement.</param>
    /// <returns>Whether the replacement follows the state machine.</returns>
    internal static bool CanReplace(ConsentRequest before, ConsentRequest after)
    {
        if (!SameRequest(before, after) || !HasSelection(before, after)) return false;
        if (before.State == ConsentRequestState.Active) return IsCodeClaim(before, after);
        if (!HasActivationPayload(after) || !HasRetention(after)) return false;
        return IsAllowedState(before.State, after.State);
    }

    static bool SameRequest(ConsentRequest before, ConsentRequest after) =>
        before.Id == after.Id && before.Binding == after.Binding && before.Deadline == after.Deadline && before.CreatedAt == after.CreatedAt &&
        before.RecoveryEpoch == after.RecoveryEpoch && after.Revision == before.Revision + 1;

    static bool HasSelection(ConsentRequest before, ConsentRequest after) =>
        (after.State != ConsentRequestState.Decided || after.Selection is not null) &&
        (after.State != ConsentRequestState.Denied || after.Selection is null) &&
        (before.State == ConsentRequestState.Pending
            ? after.State == ConsentRequestState.Decided || after.Selection is null
            : before.Selection == after.Selection);

    static bool IsCodeClaim(ConsentRequest before, ConsentRequest after) =>
        after.State == before.State && !before.CodeClaimed && after.CodeClaimed && after.AuthorizationId == before.AuthorizationId && after.PurgeAt is null;

    static bool HasActivationPayload(ConsentRequest request) =>
        !request.CodeClaimed && (request.State == ConsentRequestState.Active ? !string.IsNullOrEmpty(request.AuthorizationId) : request.AuthorizationId is null);

    static bool HasRetention(ConsentRequest request) =>
        (request.State is ConsentRequestState.Denied or ConsentRequestState.Abandoned or ConsentRequestState.EvidenceWithheld) == request.PurgeAt.HasValue;

    static bool IsAllowedState(ConsentRequestState before, ConsentRequestState after) => (before, after) switch
    {
        (ConsentRequestState.Pending, ConsentRequestState.Decided or ConsentRequestState.Denied or ConsentRequestState.Abandoned) => true,
        (ConsentRequestState.Decided, ConsentRequestState.EvidenceRecorded or ConsentRequestState.Abandoned) => true,
        (ConsentRequestState.EvidenceRecorded, ConsentRequestState.Active or ConsentRequestState.EvidenceWithheld) => true,
        _ => false
    };
}
