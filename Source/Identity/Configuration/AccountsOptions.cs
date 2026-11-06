// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Opt-in event-sourced accounts.</summary>
public class AccountsOptions
{
    /// <summary>Gets or sets whether the opt-in account services are enabled.</summary>
    public bool EventSourced { get; set; }

    /// <summary>Gets or sets the base64 lookup secret, never logged.</summary>
    public string LookupKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the single supported lookup-key revision.</summary>
    public int LookupKeyRevision { get; set; } = 1;

    /// <summary>Gets or sets the minimum explicit-link proof hold.</summary>
    public TimeSpan LinkHoldMinimum { get; set; } = TimeSpan.Zero;
}
