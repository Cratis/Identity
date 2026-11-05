// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Registration;

/// <summary>Startup-only lookup hints; the stream guard and constraint authorize writes.</summary>
/// <param name="readModels">The projection reader.</param>
public class ClientRegistrationLookups(IReadModels readModels)
{
    /// <summary>Finds an active registration by OAuth client id.</summary>
    /// <param name="clientId">The validated OAuth identity.</param>
    /// <returns>The active registration, if present.</returns>
    public virtual async Task<ClientIdLookup?> Find(ClientId clientId) => (await Active()).SingleOrDefault(row => row.ClientId == clientId);

    /// <summary>Lists active registrations.</summary>
    /// <returns>The active lookup rows.</returns>
    public virtual Task<IEnumerable<ClientIdLookup>> Active() => readModels.GetInstances<ClientIdLookup>();
}
