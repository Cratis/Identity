// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Identity.Realm;

/// <summary>Registers the realm-aware Chronicle namespace resolver.</summary>
internal static class RealmServiceCollectionExtensions
{
    /// <summary>Wraps the existing namespace resolver for Identity realms.</summary>
    /// <param name="services">The services.</param>
    internal static void AddRealmNamespaceResolver(this IServiceCollection services)
    {
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IEventStoreNamespaceResolver));
        if (previous is null)
        {
            return;
        }

        services.Remove(previous);
        services.AddSingleton<IEventStoreNamespaceResolver>(provider => new IdentityRealmNamespaceResolver(
            (IEventStoreNamespaceResolver)(previous.ImplementationInstance ?? previous.ImplementationFactory?.Invoke(provider) ??
                ActivatorUtilities.CreateInstance(provider, previous.ImplementationType!))));
    }
}
