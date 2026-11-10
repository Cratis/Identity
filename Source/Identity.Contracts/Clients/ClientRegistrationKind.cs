// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The registration origins in identity-events-v1; an origin grants no authority.</summary>
public enum ClientRegistrationKind
{
    /// <summary>A pre-registered client.</summary>
    PreRegistered = 0,

    /// <summary>A client identified through a client identifier metadata document.</summary>
    ClientIdentifierMetadataDocument = 1,

    /// <summary>A dynamically registered client.</summary>
    DynamicClientRegistration = 2
}
