// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Stateless data-protection proof, separate from the operational decision CAS.</summary>
/// <param name="protection">The host's managed data-protection key ring.</param>
/// <param name="time">The server clock.</param>
internal sealed class ConsentProof(IDataProtectionProvider protection, TimeProvider time) : IConsentProof
{
    /// <inheritdoc/>
    public ConsentProofToken Issue(ConsentRequest request) => Protector(request).Protect(Expiry(request));

    /// <inheritdoc/>
    public bool Validate(ConsentProofToken? proof, ConsentRequest request)
    {
        if (string.IsNullOrEmpty(proof?.Value) || time.GetUtcNow() >= request.Deadline) return false;
        try
        {
            return Protector(request).Unprotect(proof.Value) == Expiry(request);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    static string Expiry(ConsentRequest request) => request.Deadline.UtcTicks.ToString(CultureInfo.InvariantCulture);

    IDataProtector Protector(ConsentRequest request) => protection.CreateProtector(
        "Cratis.Identity.ConsentProof.v1", request.Id.Value.ToString("D"), request.Binding.Account.Value, request.Binding.Session.Value, request.Binding.Realm.Value);
}
