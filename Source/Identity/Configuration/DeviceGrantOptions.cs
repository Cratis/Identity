// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Bounds the RFC 8628 device authorization flow.</summary>
public class DeviceGrantOptions
{
    /// <summary>Gets or sets whether device authorization is available.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the user-code length, at least eight characters.</summary>
    public int UserCodeLength { get; set; } = 8;

    /// <summary>Gets or sets at least twenty distinct uppercase ASCII letters or digits.</summary>
    public string UserCodeCharset { get; set; } = "BCDFGHJKLMNPQRSTVWXZ";

    /// <summary>Gets or sets the lifetime of an unapproved device request.</summary>
    public TimeSpan CodeLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the initial polling interval.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the rolling verification attempt window.</summary>
    public TimeSpan VerificationWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the maximum attempts for one user code per window.</summary>
    public int AttemptsPerCode { get; set; } = 5;

    /// <summary>Gets or sets the maximum attempts for one account per window.</summary>
    public int AttemptsPerAccount { get; set; } = 5;

    /// <summary>Gets or sets the maximum attempts for one client IP per window.</summary>
    public int AttemptsPerIp { get; set; } = 30;

    /// <summary>Gets or sets the realm-wide guessing ceiling per verification window.</summary>
    public int AttemptsGlobally { get; set; } = 1000;

    /// <summary>Gets or sets the issuance allowance per trusted client IP per minute.</summary>
    public int IssuancesPerIp { get; set; } = 10;
}
