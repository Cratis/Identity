// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.given;
using Cratis.Identity.Storage;
using MongoDB.Driver;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.given;

public class a_client_registration_store : Specification
{
    protected const string Secret = "Hostile-secret-that-must-never-be-persisted-🔐";
    protected EventStoreForTesting _events;
    protected refreshing_client_registration_store _store;
    protected ClientRegistrationReconciler _writer;
    protected ClientSecretDigester _digester;
    protected ClientMaterializations _materializations;
    protected OpenIddictApplicationDescriptor _cli;
    protected OpenIddictApplicationDescriptor _confidential;
    protected List<(ClientRegistrationState State, OpenIddictApplicationDescriptor Policy)> _materialized;
    protected List<ClientRegistrationId> _removed;
    protected ReadModelScenario<ClientRegistrationState> _states;
    protected AppendFailureLog<ClientRegistrationStore> _appendLog;
    ReadModelScenario<ClientIdLookup> _lookups;
    readonly HashSet<ClientRegistrationId> _ids = [];

    void Establish()
    {
        _events = new();
        _digester = new(client_test_keys.Create());
        _materializations = Substitute.For<ClientMaterializations>(Substitute.For<IMongoDatabase>());
        _materialized = [];
        _removed = [];
        _materializations.Upsert(default!, default!, default).ReturnsForAnyArgs(call =>
        {
            _materialized.Add((call.Arg<ClientRegistrationState>(), call.Arg<OpenIddictApplicationDescriptor>()));
            return Task.CompletedTask;
        });
        _materializations.Remove(default!, default).ReturnsForAnyArgs(call =>
        {
            _removed.Add(call.Arg<ClientRegistrationId>());
            return Task.CompletedTask;
        });
        _states = new();
        _lookups = new();
        var reader = Substitute.For<IReadModels>();
        reader.GetInstances<ClientIdLookup>(default).ReturnsForAnyArgs(_ => Task.FromResult<IEnumerable<ClientIdLookup>>(_lookups.Instances.Values));
        reader.GetInstanceById<ClientRegistrationState>(default(ReadModelKey)!, default).ReturnsForAnyArgs(call => Task.FromResult(_states.InstanceForEventSourceId(call.Arg<ReadModelKey>().Value) ?? new(call.Arg<ReadModelKey>().Value)));
        _appendLog = new();
        _store = new(_events, reader, TimeProvider.System, Refresh, _appendLog);
        _writer = new(_store, new(reader), _digester, _materializations, TimeProvider.System);
        _cli = new() { ClientId = "cratis-cli", ClientType = OpenIddictConstants.ClientTypes.Public };
        _cli.RedirectUris.Add(new("http://127.0.0.1/callback"));
        _cli.RedirectUris.Add(new("http://[::1]/callback"));
        _cli.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        _cli.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        _confidential = new() { ClientId = "introspection", ClientSecret = Secret, ClientType = OpenIddictConstants.ClientTypes.Confidential };
        _confidential.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
    }

    protected Task<int> Reconcile(params OpenIddictApplicationDescriptor[] descriptors) => _writer.Reconcile(descriptors, CancellationToken.None);

    protected async Task<ClientRegistrationId> Seed(OpenIddictApplicationDescriptor descriptor)
    {
        var id = ClientRegistrationId.New();
        var now = DateTimeOffset.UtcNow;
        var facts = new List<object> { new ClientRegistered(ClientId.Create(descriptor.ClientId!), RegistrationKind.PreRegistered, now) };
        if (descriptor.RedirectUris.Count > 0)
        {
            facts.Add(new ClientRedirectUrisChanged(descriptor.RedirectUris.Select(uri => RedirectUri.Create(uri.OriginalString)).ToArray()));
        }

        if (descriptor.ClientSecret is not null)
        {
            facts.Add(new ClientSecretRotated(new(Guid.NewGuid()), _digester.Digest(ClientId.Create(descriptor.ClientId!), descriptor.ClientSecret), now, now));
        }

        var result = await _events.EventLog.AppendMany(id.Value.ToString("D"), facts);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Could not seed the client fixture.");
        }

        await Refresh(id);
        return id;
    }

    protected async Task Refresh(ClientRegistrationId id)
    {
        _ids.Add(id);
        _states.Dispose();
        _lookups.Dispose();
        _states = new();
        _lookups = new();
        var types = new[] { typeof(ClientRegistered), typeof(ClientRedirectUrisChanged), typeof(ClientSecretRotated), typeof(ClientRetired) }.Select(_events.EventTypes.GetEventTypeFor).ToArray();
        foreach (var source in _ids)
        {
            var events = await _events.EventLog.GetForEventSourceIdAndEventTypes(source.Value.ToString("D"), types);
            await _states.ProcessAppendedEvents(events);
            await _lookups.ProcessAppendedEvents(events);
        }

        _ = _states.Instances;
        _ = _lookups.Instances;
    }

    void Destroy()
    {
        _states.Dispose();
        _lookups.Dispose();
        _events.Dispose();
    }
}
