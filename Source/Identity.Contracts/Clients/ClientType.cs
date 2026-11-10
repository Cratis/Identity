// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Clients;

/// <summary>The client types approved by the v1.1 amendment.</summary>
public enum ClientType
{
    /// <summary>A public client.</summary>
    Public = 0,

    /// <summary>A confidential client.</summary>
    Confidential = 1
}
