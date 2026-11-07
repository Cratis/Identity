// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>Why a legacy account store could not be checked.</summary>
internal enum LegacyStoreFailure
{
    /// <summary>The check did not finish in time.</summary>
    Timeout = 0,

    /// <summary>The server could not be reached.</summary>
    Unreachable = 1,

    /// <summary>The server refused the credentials or the permissions.</summary>
    AuthenticationFailed = 2,

    /// <summary>Any other failure, including a malformed connection string.</summary>
    Error = 3
}
