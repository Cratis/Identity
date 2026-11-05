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
    public async Task<AccountSubject> SignIn(VerifiedAuthentication authentication, Email email, PersonalName name, Login login)
    {
        var age = time.GetUtcNow() - authentication.AuthTime;
        if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(5))
        {
            throw new AccountOperationRefused("fresh_upstream_authentication_required");
        }
        var hash = digester.Digest(authentication.IdpIssuer, authentication.Subject);
        var reservation = await lookups.Find(hash);
        var account = reservation?.AccountSubject ?? AccountSubject.New();
        var read = await store.Read(account);
        var events = new List<object>();
        LinkId link;
        if (reservation is null)
        {
            if (read.State is { Registered: true } or { Tombstoned: true })
            {
                throw new AccountOperationRefused("subject_already_reserved");
            }
            link = Guid.NewGuid();
            events.Add(new AccountRegistrationRecorded(time.GetUtcNow()));
            events.Add(new UpstreamKeyLinkRecorded(link, authentication.ConnectionId, authentication.IdpIssuer, hash, Guid.NewGuid(), time.GetUtcNow()));
            events.Add(new UpstreamKeySubjectRecorded(link, authentication.IdpIssuer, authentication.Subject));
        }
        else
        {
            UpstreamKeyLinking.RequireActive(read.State);
            link = reservation.LinkId;
            if (!reservation.Active || !read.State.Links.TryGetValue(link.Value, out var confirmed) || confirmed.KeyHash != hash)
            {
                throw new AccountOperationRefused("upstream_key_reserved_but_inactive");
            }
        }
        if (read.State.Email != email)
        {
            events.Add(new AccountEmailChanged(email));
        }
        if (read.State.Name != name)
        {
            events.Add(new AccountNameChanged(name));
        }
        if (read.State.Logins.GetValueOrDefault(link.Value) != login)
        {
            events.Add(new UpstreamKeyDisplayIdentifierObserved(link, login, time.GetUtcNow()));
        }
        await store.Append(account, read, events);
        return account;
    }
}
