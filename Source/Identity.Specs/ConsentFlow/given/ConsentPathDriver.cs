// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Pending;

namespace Cratis.Identity.ConsentFlow.given;

/// <summary>
/// Spec-side seam over the dormant consent path (Arc commands, interaction boundary and activation barrier) so the
/// behaviour of the path can be pinned once and later re-pointed at the replacement core without rewriting the specs.
/// </summary>
public class ConsentPathDriver : Pending.given.a_consent_interaction
{
    internal int AppendedFacts => evidence.Appends;

    internal int PreparedAuthorizations => authorizations.Count;

    /// <summary>Switches the session the caller resolves to; a different session is a different principal context.</summary>
    /// <param name="session">The session identifier the caller now presents.</param>
    internal void As(string session) => sessions.SessionId.Returns(session);

    internal Task<ConsentRequest> Request(ConsentBinding? forBinding = null) => activation.Create(forBinding ?? binding, CancellationToken.None);

    internal Task<PendingConsent?> Pending() => interaction.Find(CancellationToken.None);

    internal async Task<bool> Refused(Func<Task> decision)
    {
        try
        {
            await decision();
            return false;
        }
        catch (ConsentDecisionRefused)
        {
            return true;
        }
    }

    internal Task<ConsentRequest?> Reconcile(ConsentRequestId? id = null) => activation.Resume(id ?? request.Id, CancellationToken.None);

    internal Task<string?> ClaimCode(ConsentRequestId? id = null) => activation.ClaimCode(id ?? request.Id, binding, CancellationToken.None);

    internal async Task<ConsentRequestState> State(ConsentRequestId? id = null) => (await store.Read(id ?? request.Id, CancellationToken.None)).State;

    internal Task<bool> AuthorizationUsable(ConsentRequestId? id = null) => authorizations.Usable(id ?? request.Id, store, fence);
}
