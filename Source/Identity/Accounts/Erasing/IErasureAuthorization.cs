// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>Decides who may request erasure of an account.</summary>
internal interface IErasureAuthorization
{
    /// <summary>Decides whether the requester may erase the account.</summary>
    /// <param name="requester">The authenticated requester.</param>
    /// <param name="account">The account to erase.</param>
    /// <returns>True when allowed.</returns>
    Task<bool> IsAllowed(ClaimsPrincipal requester, AccountSubject account);
}
