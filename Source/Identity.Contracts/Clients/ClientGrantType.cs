// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The grant permissions used by Identity's current descriptors and server.</summary>
public enum ClientGrantType
{
    /// <summary>The authorization_code grant.</summary>
    AuthorizationCode = 0,

    /// <summary>The refresh_token grant.</summary>
    RefreshToken = 1,

    /// <summary>The urn:ietf:params:oauth:grant-type:device_code grant.</summary>
    DeviceCode = 2,

    /// <summary>The urn:ietf:params:oauth:grant-type:token-exchange grant.</summary>
    TokenExchange = 3
}
