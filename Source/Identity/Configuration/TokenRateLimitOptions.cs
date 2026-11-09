// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>
/// Per-process, fixed-window admission limits for the token endpoint.
/// </summary>
public class TokenRateLimitOptions
{
    /// <summary>Gets or sets whether token endpoint admission is limited.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the window, positive and at most one hour.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the request budget per source address or IPv6 /64.</summary>
    public int PerSourceRequests { get; set; } = 300;

    /// <summary>Gets or sets the request budget per claimed client within a source.</summary>
    public int PerClientRequests { get; set; } = 120;

    /// <summary>Gets or sets the additional token-exchange budget per claimed client within a source.</summary>
    public int PerClientExchangeRequests { get; set; } = 30;

    /// <summary>Gets or sets the maximum tracked keys per kind, from 100 through 1,000,000.</summary>
    public int MaxTrackedKeys { get; set; } = 10_000;
}
