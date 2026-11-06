// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts;

/// <summary>The separate event-sourced account path; it does not implement or replace Mongo IAccounts.</summary>
/// <param name="store">The guarded account store.</param>
/// <param name="lookups">The immutable-key lookup.</param>
/// <param name="digester">The keyed digester.</param>
/// <param name="time">The trusted clock.</param>
public class EventSourcedAccounts(AccountStore store, UpstreamLookups lookups, UpstreamKeyDigester digester, TimeProvider time)
{
    /// <summary>Finds only by exact verified upstream pair, or atomically allocates and claims a new subject.</summary>
    /// <param name="authentication">The adapter-verified immutable key.</param>
    /// <param name="email">Mutable profile email, never matching proof.</param>
    /// <param name="name">Mutable profile name.</param>
    /// <param name="login">Mutable per-key display identifier.</param>
    /// <returns>The immutable account subject.</returns>
    /// <exception cref="AccountOperationRefused">The proof, account or key reservation is not usable.</exception>
    public async Task<AccountSubject> SignIn(VerifiedAuthentication authentication, Email email, PersonalName name, Login login)
    {
        RequireFresh(authentication);
        var hash = digester.Digest(authentication.IdpIssuer, authentication.Subject);
        var reservation = await lookups.Find(hash);
        var account = reservation?.AccountSubject ?? new AccountSubject(authentication.OperationId.Value.ToString("D"));
        var read = await store.Read(account);
        var events = new List<object>();
        var link = reservation is null ? Claim(authentication, hash, read.State, events) : ExistingLink(reservation, read.State, hash);
        events.AddRange(ProfileChanges(read.State, link, new(email, name, login), time.GetUtcNow()));
        await store.Append(account, read, events);
        return account;
    }

    static LinkId ExistingLink(UpstreamKeyMatch reservation, AccountLinkState state, UpstreamLookupDigest hash)
    {
        UpstreamKeyLinking.RequireActive(state);
        if (!reservation.Active || !state.Links.TryGetValue(reservation.LinkId.Value, out var confirmed))
        {
            throw new AccountOperationRefused("upstream_key_reserved_but_inactive");
        }

        if (confirmed.KeyHash != hash)
        {
            throw new AccountOperationRefused("upstream_key_reserved_but_inactive");
        }

        return reservation.LinkId;
    }

    static IEnumerable<object> ProfileChanges(AccountLinkState state, LinkId link, ObservedProfile profile, DateTimeOffset now)
    {
        if (profile.Email is { } email && state.Email != email)
        {
            yield return new AccountEmailChanged(email);
        }

        if (state.Name != profile.Name)
        {
            yield return new AccountNameChanged(profile.Name);
        }

        if (state.Logins.GetValueOrDefault(link.Value) != profile.Login)
        {
            yield return new UpstreamKeyDisplayIdentifierObserved(link, profile.Login, now);
        }
    }

    LinkId Claim(VerifiedAuthentication authentication, UpstreamLookupDigest hash, AccountLinkState state, List<object> events)
    {
        if (state.Registered || state.Tombstoned)
        {
            throw new AccountOperationRefused("subject_already_reserved");
        }

        LinkId link = FactId.ForOperation(authentication.OperationId, nameof(UpstreamKeyLinkRecorded)).Value;
        events.Add(new AccountRegistrationRecorded(authentication.AuthTime, FactId.ForOperation(authentication.OperationId, nameof(AccountRegistrationRecorded))));
        events.Add(new UpstreamKeyLinkRecorded(link, authentication.ConnectionId, authentication.IdpIssuer, hash, authentication.OperationId.Value, authentication.AuthTime, FactId.ForOperation(authentication.OperationId, nameof(UpstreamKeyLinkRecorded))));
        events.Add(new UpstreamKeySubjectRecorded(link, authentication.IdpIssuer, authentication.Subject));
        return link;
    }

    void RequireFresh(VerifiedAuthentication authentication)
    {
        var age = time.GetUtcNow() - authentication.AuthTime;
        if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(5))
        {
            throw new AccountOperationRefused("fresh_upstream_authentication_required");
        }
    }
}
