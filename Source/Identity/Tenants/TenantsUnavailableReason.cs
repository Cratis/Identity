// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>
/// Defines the failure categories of a tenants lookup.
/// </summary>
internal enum TenantsUnavailableReason
{
    /// <summary>No product callback is configured.</summary>
    NotConfigured = 0,

    /// <summary>The product cannot be reached or returned an unsuccessful status.</summary>
    Unreachable = 1,

    /// <summary>The callback deadline elapsed.</summary>
    TimedOut = 2,

    /// <summary>The product refused the callback credential.</summary>
    Refused = 3,

    /// <summary>The response does not satisfy the callback contract.</summary>
    Malformed = 4,

    /// <summary>The response belongs to a different subject.</summary>
    WrongSubject = 5
}
