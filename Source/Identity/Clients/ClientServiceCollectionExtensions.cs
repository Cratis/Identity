// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>Registers configured client descriptors.</summary>
internal static class ClientServiceCollectionExtensions
{
    /// <summary>Adds client descriptors.</summary>
    /// <param name="services">The services.</param>
    internal static void AddClients(this IServiceCollection services) => services.AddSingleton<ClientDescriptors>();
}
