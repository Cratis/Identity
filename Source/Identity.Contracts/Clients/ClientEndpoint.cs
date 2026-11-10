// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The endpoint permissions enabled by Identity's current descriptors and server.</summary>
public enum ClientEndpoint
{
    /// <summary>The authorization endpoint.</summary>
    Authorization = 0,

    /// <summary>The token endpoint.</summary>
    Token = 1,

    /// <summary>The revocation endpoint.</summary>
    Revocation = 2,

    /// <summary>The introspection endpoint.</summary>
    Introspection = 3,

    /// <summary>The device authorization endpoint.</summary>
    DeviceAuthorization = 4
}
