// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Identity.Accounts.given;

public class FixedAccountPrincipal(ClaimsPrincipal principal) : ICurrentPrincipalAccessor
{
    public ClaimsPrincipal? Current => principal;
}
