// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Identity.Integration.given;

/// <summary>Supplies an application namespace that Identity must never inherit for realm facts.</summary>
public sealed class SuppliedTenantNamespace : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve() => "tenant-alpha";
}
