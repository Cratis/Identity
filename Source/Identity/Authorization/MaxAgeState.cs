// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// The outcome of inspecting the maximum-age parameter of an authorization request.
/// </summary>
internal enum MaxAgeState
{
    /// <summary>The request carries no maximum-age parameter.</summary>
    Absent = 0,

    /// <summary>The request carries exactly one well-formed, non-negative maximum-age value.</summary>
    Valid = 1,

    /// <summary>The request carries a malformed, repeated or differently-cased maximum-age parameter.</summary>
    Invalid = 2
}
