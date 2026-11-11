// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.Authorization;

/// <summary>Binds a short consent round trip to the request whose original authentication age was accepted.</summary>
/// <param name="protection">The site's shared Data Protection provider.</param>
/// <param name="timeProvider">The authentication clock.</param>
internal sealed class OidcAuthenticationProof(IDataProtectionProvider protection, TimeProvider timeProvider)
{
    /// <summary>The protected form field; it grants no remembered consent.</summary>
    public const string Field = "oidc_authentication";
    readonly IDataProtector _protector = protection.CreateProtector("Cratis.Identity.Oidc.ConsentAuthentication.v1");

    /// <summary>Records that this exact authorization request passed its authentication-age check.</summary>
    /// <param name="context">The initial authorization request.</param>
    /// <param name="authentication">The accepted site authentication.</param>
    /// <returns>A protected, five-minute request binding.</returns>
    public string Issue(HttpContext context, SignedInAccount authentication) => _protector.Protect(
        $"{timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}:{Fingerprint(context, authentication)}");

    /// <summary>Validates the consent round trip without treating cookie renewal as fresh authentication.</summary>
    /// <param name="context">The submitted consent form.</param>
    /// <param name="authentication">The still-current site authentication.</param>
    /// <returns>Whether this form belongs to a recently accepted authentication request.</returns>
    public bool IsValid(HttpContext context, SignedInAccount authentication)
    {
        if (!context.Request.HasFormContentType) return false;
        var protectedValue = context.Request.Form[Field].ToString();
        if (string.IsNullOrEmpty(protectedValue)) return false;
        try
        {
            return Matches(_protector.Unprotect(protectedValue), context, authentication);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return false;
        }
    }

    bool Matches(string value, HttpContext context, SignedInAccount authentication)
    {
        var parts = value.Split(':', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var issued)) return false;
        var age = timeProvider.GetUtcNow().ToUnixTimeSeconds() - issued;
        if (age is < 0 or > 300) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[1]), Encoding.UTF8.GetBytes(Fingerprint(context, authentication)));
    }

    static string Fingerprint(HttpContext context, SignedInAccount authentication)
    {
        var parameters = AuthorizationParameters.From(context.Request).Where(parameter => parameter.Key != Field)
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal).ThenBy(parameter => parameter.Value, StringComparer.Ordinal).ToArray();
        var binding = JsonSerializer.Serialize(new { parameters, subject = authentication.Account.Id.Value, authTime = authentication.AuthTime.ToUnixTimeSeconds(), sid = authentication.SessionId });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(binding)));
    }
}
