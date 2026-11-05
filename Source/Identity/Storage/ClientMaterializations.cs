// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Storage;

/// <summary>Idempotently materializes approved client facts, never writes configuration facts.</summary>
/// <param name="database">The operational database.</param>
public class ClientMaterializations(IMongoDatabase database)
{
    /// <summary>Derives the stable ObjectId required by OpenIddict token and authorization references.</summary>
    /// <param name="id">The opaque registration identity.</param>
    /// <returns>The first twelve UUID bytes in canonical wire order.</returns>
    public static ObjectId ApplicationId(ClientRegistrationId id) => new(Convert.FromHexString(id.Value.ToString("N"))[..12]);

    /// <summary>Projects fact identity, redirects and comparand alongside current server policy.</summary>
    /// <param name="state">The committed passive state.</param>
    /// <param name="policy">The matching descriptor, used only for non-secret policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion task.</returns>
    /// <exception cref="InvalidOperationException">The supplied state has no active registration.</exception>
    public virtual async Task Upsert(ClientRegistrationState state, OpenIddictApplicationDescriptor policy, CancellationToken cancellationToken)
    {
        if (state.Retired || state.ClientId is null)
        {
            throw new InvalidOperationException("Only active client facts can be materialized.");
        }

        var document = new OpenIddictMongoDbApplication
        {
            Id = ApplicationId(state.ClientRegistrationId),
            ClientId = state.ClientId.Value,
            RedirectUris = state.RedirectUris.Select(uri => uri.Value).ToArray(),
            ClientSecret = policy.ClientType == OpenIddictConstants.ClientTypes.Confidential ? state.CurrentSecretHash?.Value : null,
            ClientType = policy.ClientType,
            ApplicationType = policy.ApplicationType,
            ConsentType = policy.ConsentType,
            DisplayName = policy.DisplayName,
            Permissions = policy.Permissions.ToArray(),
            Requirements = policy.Requirements.ToArray(),
            Properties = new BsonDocument("cratis_client_registration_id", state.Id)
        };

        var collection = Collection();
        await collection.DeleteManyAsync(application => application.ClientId == document.ClientId && application.Id != document.Id, cancellationToken);
        await collection.ReplaceOneAsync(application => application.Id == document.Id, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    /// <summary>Removes a retired registration's operational copy.</summary>
    /// <param name="id">The retired stream identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion task.</returns>
    public virtual Task Remove(ClientRegistrationId id, CancellationToken cancellationToken) => Collection().DeleteOneAsync(application => application.Id == ApplicationId(id), cancellationToken);

    IMongoCollection<OpenIddictMongoDbApplication> Collection() => database.GetCollection<OpenIddictMongoDbApplication>("openiddict.applications");
}
