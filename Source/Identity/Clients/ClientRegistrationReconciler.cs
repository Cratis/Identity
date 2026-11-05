// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Storage;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients;

/// <summary>The scoped startup writer: append facts before changing any operational copy.</summary>
/// <param name="store">The guarded fact boundary.</param>
/// <param name="lookups">Active identity hints.</param>
/// <param name="digester">The secret-custody digester.</param>
/// <param name="materializations">The operational sink.</param>
/// <param name="time">The trusted clock.</param>
public class ClientRegistrationReconciler(ClientRegistrationStore store, ClientRegistrationLookups lookups, ClientSecretDigester digester, ClientMaterializations materializations, TimeProvider time)
{
    /// <summary>Reconciles all declared clients, validating the complete input before the first append.</summary>
    /// <param name="descriptors">The operator-declared client policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of declared clients.</returns>
    /// <exception cref="InvalidClientValue">A configured identity is invalid or duplicated.</exception>
    public async Task<int> Reconcile(IEnumerable<OpenIddictApplicationDescriptor> descriptors, CancellationToken cancellationToken)
    {
        var intended = descriptors.Select(descriptor => IntendedClientRegistration.Create(descriptor, digester)).ToArray();
        if (intended.Select(client => client.ClientId).Distinct().Count() != intended.Length)
        {
            throw new InvalidClientValue("duplicate client id");
        }

        foreach (var client in intended)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RegisterOrUpdate(client, cancellationToken);
        }

        var declared = intended.Select(client => client.ClientId).ToHashSet();
        foreach (var stale in (await lookups.Active()).Where(row => !declared.Contains(row.ClientId)))
        {
            await Retire(stale.Id, cancellationToken);
        }

        return intended.Length;
    }

    async Task RegisterOrUpdate(IntendedClientRegistration client, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var lookup = await lookups.Find(client.ClientId);
            var id = lookup?.Id ?? ClientRegistrationId.New();
            var read = await store.Read(id);
            if (read.State.Retired)
            {
                continue;
            }

            var events = Differences(client, read.State);
            if (events.Count > 0 && await store.Append(id, events, read) != ClientRegistrationOutcome.Success)
            {
                continue;
            }

            var committed = events.Count == 0 ? read.State : (await store.Read(id)).State;
            await materializations.Upsert(committed, client.Policy, cancellationToken);
            return;
        }

        throw new InvalidOperationException("Client registration changed concurrently; reconciliation failed closed.");
    }

    List<object> Differences(IntendedClientRegistration client, ClientRegistrationState state)
    {
        var events = new List<object>();
        var now = time.GetUtcNow();
        if (state.ClientId is null)
        {
            events.Add(new ClientRegistered(client.ClientId, RegistrationKind.PreRegistered, now));
        }

        if (!state.RedirectUris.ToHashSet().SetEquals(client.RedirectUris))
        {
            events.Add(new ClientRedirectUrisChanged(client.RedirectUris));
        }

        if (client.Digest is not null && (state.CurrentSecretHash is null || !digester.Matches(client.ClientId, client.Policy.ClientSecret!, state.CurrentSecretHash)))
        {
            events.Add(new ClientSecretRotated(OpaqueReferenceId.New(), client.Digest, now, now));
        }

        return events;
    }

    async Task Retire(ClientRegistrationId id, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var read = await store.Read(id);
            if (read.State.Retired || await store.Append(id, [new ClientRetired(new ReasonId("undeclared"), time.GetUtcNow())], read) == ClientRegistrationOutcome.Success)
            {
                await materializations.Remove(id, cancellationToken);
                return;
            }
        }

        throw new InvalidOperationException("Client retirement changed concurrently; reconciliation failed closed.");
    }
}
