// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Identity.Realm;

/// <summary>Pins Identity work to the realm while preserving host tenant resolution elsewhere.</summary>
/// <param name="inner">The host's namespace resolver.</param>
internal sealed class IdentityRealmNamespaceResolver(IEventStoreNamespaceResolver inner) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve() => IdentityRealmScope.IsActive ? EventStoreNamespaceName.Default : inner.Resolve();
}
