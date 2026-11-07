// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>The protocol-visible account lifecycle state.</summary>
public enum AccountStatus
{
    /// <summary>The account can authenticate.</summary>
    Active = 0,

    /// <summary>The account is disabled.</summary>
    Disabled = 1,

    /// <summary>The account was permanently tombstoned.</summary>
    Tombstoned = 2
}
