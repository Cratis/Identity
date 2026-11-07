// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores;

/// <summary>
/// Provides the application and scope read models used by the protocol stores.
/// </summary>
internal interface IProtocolCatalog
{
    /// <summary>Gets the applications known to the realm.</summary>
    IReadOnlyList<ClientApplication> Applications { get; }

    /// <summary>Gets the scopes known to the realm.</summary>
    IReadOnlyList<ResourceScope> Scopes { get; }
}
