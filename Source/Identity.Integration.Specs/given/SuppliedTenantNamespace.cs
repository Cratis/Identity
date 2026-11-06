// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Tenancy;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;

namespace Cratis.Identity.Integration.given;

/// <summary>Uses the real Arc request tenant resolver that Identity must not inherit for realm facts.</summary>
/// <param name="tenant">The current request's tenant accessor.</param>
public sealed class SuppliedTenantNamespace(ITenantIdAccessor tenant) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve() => new TenantNamespaceResolver(tenant).Resolve();
}
