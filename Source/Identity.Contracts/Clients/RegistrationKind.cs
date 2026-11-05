// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The registration origin; members are pinned by the locked v1 catalogue.</summary>
public enum RegistrationKind
{
    /// <summary>Operator-declared configuration.</summary>
    PreRegistered = 0,

    /// <summary>A client-id metadata document accepted by policy.</summary>
    ClientIdMetadataDocument = 1,

    /// <summary>A dynamic registration accepted by policy.</summary>
    DynamicRegistration = 2
}
