// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>Folds only Internal facts for guarded account decisions.</summary>
internal class AccountLinkStateReducer : IReducerFor<AccountLinkState>
{
    /// <summary>Gets the reducer identity.</summary>
    public ReducerId Id => "47c30545-5546-433a-828a-b7ba31fd9710";

    /// <summary>Allocates a subject.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountRegistrationRecorded @event, AccountLinkState? current, EventContext context)
    {
        _ = @event;
        return Start(current, context) with { Registered = true };
    }

    /// <summary>Updates profile email.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountEmailChanged @event, AccountLinkState? current, EventContext context) => Start(current, context) with { Email = @event.Email };

    /// <summary>Updates profile name.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountNameChanged @event, AccountLinkState? current, EventContext context) => Start(current, context) with { Name = @event.PersonalName };

    /// <summary>Updates a display-only identifier.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyDisplayIdentifierObserved @event, AccountLinkState? current, EventContext context)
    {
        var state = Start(current, context);
        var logins = state.Logins.ToDictionary();
        logins[@event.LinkId.Value] = @event.Login;
        return state with { Logins = logins };
    }

    /// <summary>Records an explicit proposal.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyLinkProposed @event, AccountLinkState? current, EventContext context)
    {
        var state = Start(current, context);
        var proposals = state.Proposals.ToDictionary();
        proposals[@event.LinkProposalId.Value] = new(@event.LinkProposalId, @event.ConnectionId, @event.IdpIssuer, @event.IdpSub, context.Occurred, context.Occurred, "pending_proof");
        return state with { Proposals = proposals };
    }

    /// <summary>Enters quarantine without granting access.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyLinkQuarantined @event, AccountLinkState? current, EventContext context) => Change(Start(current, context), @event.LinkProposalId.Value, "quarantined", @event.HoldNotBefore);

    /// <summary>Records resolver clearance.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyLinkCleared @event, AccountLinkState? current, EventContext context) => Change(Start(current, context), @event.LinkProposalId.Value, "cleared");

    /// <summary>Records terminal rejection.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyLinkRejected @event, AccountLinkState? current, EventContext context) => Change(Start(current, context), @event.LinkProposalId.Value, "rejected");

    /// <summary>Confirms a link.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyLinkRecorded @event, AccountLinkState? current, EventContext context)
    {
        var state = Change(Start(current, context), @event.LinkId.Value, "confirmed");
        var links = state.Links.ToDictionary();
        links[@event.LinkId.Value] = @event;
        return state with { Links = links };
    }

    /// <summary>Unlinks without freeing the key reservation.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(UpstreamKeyUnlinkRecorded @event, AccountLinkState? current, EventContext context)
    {
        var state = Start(current, context);
        var links = state.Links.ToDictionary();
        links.Remove(@event.LinkId.Value);
        return state with { Links = links };
    }

    /// <summary>Disables authentication.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountDisablementRecorded @event, AccountLinkState? current, EventContext context)
    {
        _ = @event;
        return Start(current, context) with { Disabled = true };
    }

    /// <summary>Re-enables without restoring grants.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountEnablementRecorded @event, AccountLinkState? current, EventContext context)
    {
        _ = @event;
        return Start(current, context) with { Disabled = false };
    }

    /// <summary>Permanently reserves a subject.</summary>
    /// <param name="event">The fact.</param>
    /// <param name="current">The prior state.</param>
    /// <param name="context">The source.</param>
    /// <returns>The next state.</returns>
    public AccountLinkState On(AccountTombstoneRecorded @event, AccountLinkState? current, EventContext context)
    {
        _ = @event;
        return Start(current, context) with { Tombstoned = true };
    }

    static AccountLinkState Start(AccountLinkState? current, EventContext context) => current ?? new(context.EventSourceId.Value);

    static AccountLinkState Change(AccountLinkState state, Guid id, string status, DateTimeOffset? hold = null)
    {
        var proposals = state.Proposals.ToDictionary();
        if (proposals.TryGetValue(id, out var proposal))
        {
            proposals[id] = proposal with { State = status, HoldNotBefore = hold ?? proposal.HoldNotBefore };
        }

        return state with { Proposals = proposals };
    }
}
