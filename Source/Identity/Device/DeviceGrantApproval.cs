// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.Device;

/// <summary>Short-lived consent evidence bound to the displayed request, account and approving browser session.</summary>
/// <param name="protection">The host-owned, shared Data Protection key ring.</param>
internal sealed class DeviceGrantApproval(IDataProtectionProvider protection)
{
    readonly ITimeLimitedDataProtector _protector = protection.CreateProtector("Cratis.Identity.DeviceApproval.v1").ToTimeLimitedDataProtector();

    /// <summary>Protects a snapshot for five minutes; code expiry is checked independently.</summary>
    /// <param name="binding">The full consent binding.</param>
    /// <returns>Opaque form evidence, never placed in a URL.</returns>
    public string Create(string binding) => _protector.Protect(binding, TimeSpan.FromMinutes(5));

    /// <summary>Rejects missing, expired, tampered or differently bound consent.</summary>
    /// <param name="proof">The form evidence.</param>
    /// <param name="binding">The independently recomputed binding.</param>
    /// <returns>Whether the browser is approving the displayed request.</returns>
    public bool Matches(string? proof, string binding)
    {
        if (string.IsNullOrEmpty(proof)) return false;
        try
        {
            return string.Equals(_protector.Unprotect(proof, out _), binding, StringComparison.Ordinal);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
