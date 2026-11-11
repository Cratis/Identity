// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Configuration;

/// <summary>
/// Validates <see cref="IdentityOptions"/> at startup, so a misconfigured resource, scope or tenant stops the
/// service instead of silently narrowing or widening what tokens can carry.
/// </summary>
/// <param name="environment">The host environment; configured tenants are Development fixtures only.</param>
internal class IdentityOptionsValidation(IHostEnvironment environment) : IValidateOptions<IdentityOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, IdentityOptions options)
    {
        var failures = new List<string>();
        ValidateHost(options, failures);
        ValidateResources(options, failures);
        ValidateDPoP(options.DPoP, environment.IsDevelopment(), failures);
        ValidateDeviceGrant(options.DeviceGrant, failures);
        failures.AddRange(TokenExchangeOptionsValidation.Validate(options, environment.IsDevelopment()));
        failures.AddRange(OidcClientOptionsValidation.Validate(options));
        failures.AddRange(TokenRateLimitOptionsValidation.Validate(options.TokenRateLimit));
        ValidateAccounts(options.Accounts, failures);
        failures.AddRange(IdentityOptionsClientsValidation.Validate(options.Clients.MetadataDocuments));
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    static void ValidateHost(IdentityOptions options, List<string> failures)
    {
        if (options.SecurityOwners.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("Identity:SecurityOwners must contain only non-empty subjects.");
        }

        ValidateIssuer(options.Issuer, failures);

        if (options.SecretStore.ReloadInterval < TimeSpan.FromSeconds(1) || options.SecretStore.ReloadInterval > TimeSpan.FromMinutes(5))
        {
            failures.Add("The secret store reload interval must be between one second and five minutes.");
        }

        if (options.Tokens.IdentityTokenLifetime <= TimeSpan.Zero || options.Tokens.IdentityTokenLifetime > TimeSpan.FromHours(1))
        {
            failures.Add("Identity:Tokens:IdentityTokenLifetime must be positive and no greater than 01:00:00.");
        }

        if (options.Tokens.AccessTokenLifetime <= TimeSpan.Zero || options.Tokens.AccessTokenLifetime > TimeSpan.FromMinutes(10))
        {
            failures.Add("Identity:Tokens:AccessTokenLifetime must be positive and no greater than 00:10:00.");
        }
    }

    static void ValidateIssuer(string configured, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(configured)) return;
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var issuer) || issuer.Scheme is not ("http" or "https"))
        {
            failures.Add($"Issuer \"{configured}\" is not an absolute HTTP URI.");
        }
        else if (!string.Equals(configured, $"{issuer.GetLeftPart(UriPartial.Authority)}/", StringComparison.Ordinal))
        {
            // Discovery is served at the root of the public origin, not under an issuer path prefix.
            failures.Add($"Issuer \"{configured}\" must be the root of its origin and end with a slash.");
        }
    }

    static void ValidateDPoP(DPoPOptions dpop, bool development, List<string> failures)
    {
        if (!dpop.Enabled) return;
        if (!ValidDPoPEndpoint(dpop.PublicTokenEndpoint, development))
        {
            failures.Add("Identity:DPoP requires an absolute public HTTPS token endpoint without user information, query or fragment (Development permits loopback HTTP).");
        }

        if (dpop.RequiredClients.Any(client => client != Clients.ClientDescriptors.CratisCli))
        {
            failures.Add("Identity:DPoP required clients must be registered token clients.");
        }

        if (!FixedDPoPWindows(dpop))
        {
            failures.Add("Identity:DPoP requires the fixed 300-second proof lifetime, 30-second future skew and 390-second replay TTL.");
        }
    }

    static bool ValidDPoPEndpoint(string configured, bool development)
    {
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var endpoint)) return false;
        if (!string.IsNullOrEmpty(endpoint.Query)) return false;
        if (!string.IsNullOrEmpty(endpoint.Fragment)) return false;
        if (!string.IsNullOrEmpty(endpoint.UserInfo)) return false;
        if (endpoint.Scheme == "https") return true;
        if (!development) return false;
        return endpoint.Scheme == "http" && endpoint.IsLoopback;
    }

    static bool FixedDPoPWindows(DPoPOptions dpop)
    {
        if (dpop.ProofLifetime != TimeSpan.FromSeconds(300)) return false;
        if (dpop.FutureSkew != TimeSpan.FromSeconds(30)) return false;
        return dpop.ReplayTtl == TimeSpan.FromSeconds(390);
    }

    static void ValidateDeviceGrant(DeviceGrantOptions device, List<string> failures)
    {
        if (!ValidDeviceUserCode(device))
        {
            failures.Add("Identity:Device user codes require 8–32 characters from at least twenty distinct uppercase ASCII letters or digits.");
        }

        if (device.CodeLifetime < TimeSpan.FromMinutes(1) || device.CodeLifetime > TimeSpan.FromMinutes(15))
        {
            failures.Add("The device code lifetime must be between one and fifteen minutes.");
        }

        if (!ValidDevicePollingInterval(device.PollingInterval))
        {
            failures.Add("The device polling interval must be a whole number of seconds between five and sixty.");
        }

        if (!ValidDeviceVerification(device))
        {
            failures.Add("Device verification needs a one-to-sixty-minute window and bounded positive code, account, IP, global and issuance limits.");
        }
    }

    static bool ValidDeviceUserCode(DeviceGrantOptions device) =>
        device.UserCodeLength is >= 8 and <= 32 && ValidDeviceUserCodeCharset(device.UserCodeCharset);

    static bool ValidDeviceUserCodeCharset(string charset) =>
        !string.IsNullOrEmpty(charset) && charset.Distinct().Count() == charset.Length && charset.Length >= 20 &&
        charset.All(_ => char.IsAsciiLetterUpper(_) || char.IsAsciiDigit(_));

    static bool ValidDevicePollingInterval(TimeSpan interval) =>
        interval >= TimeSpan.FromSeconds(5) && interval <= TimeSpan.FromSeconds(60) && interval.Ticks % TimeSpan.TicksPerSecond == 0;

    static bool ValidDeviceVerification(DeviceGrantOptions device) =>
        ValidDeviceVerificationWindow(device) && ValidDeviceAttemptLimits(device) && ValidDeviceIssuanceLimits(device);

    static bool ValidDeviceVerificationWindow(DeviceGrantOptions device) =>
        device.VerificationWindow >= TimeSpan.FromMinutes(1) && device.VerificationWindow <= TimeSpan.FromHours(1);

    static bool ValidDeviceAttemptLimits(DeviceGrantOptions device) =>
        device.AttemptsPerCode is >= 1 and <= 10 && device.AttemptsPerAccount is >= 1 and <= 100 && device.AttemptsPerIp is >= 1 and <= 300;

    static bool ValidDeviceIssuanceLimits(DeviceGrantOptions device) =>
        device.AttemptsGlobally is >= 1 and <= 1000 && device.IssuancesPerIp is >= 1 and <= 100;

    static IEnumerable<string> ValidateResource(ResourceServerOptions resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Audience))
        {
            yield return "A resource server has no audience.";
        }

        foreach (var failure in ValidateResourceIndicators(resource)) yield return failure;
        foreach (var failure in ValidateResourceScopes(resource)) yield return failure;
        foreach (var tenant in resource.Tenants.Where(_ => string.IsNullOrWhiteSpace(_.Id)))
        {
            yield return $"A tenant of \"{resource.Audience}\" named \"{tenant.Name}\" has no id.";
        }
    }

    static IEnumerable<string> ValidateResourceIndicators(ResourceServerOptions resource)
    {
        if (resource.Indicators.Count == 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has no resource indicators.";
        }

        if (resource.Indicators.Count > 1)
        {
            yield return $"Resource server \"{resource.Audience}\" must have exactly one resource indicator.";
        }

        foreach (var indicator in resource.Indicators.Where(_ => ResourceIndicator.Normalize(_) is null))
        {
            yield return $"Resource indicator \"{indicator}\" of \"{resource.Audience}\" is not an absolute URI without a fragment.";
        }
    }

    static IEnumerable<string> ValidateResourceScopes(ResourceServerOptions resource)
    {
        if (resource.Scopes.Count == 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has no scopes.";
        }

        foreach (var scope in resource.Scopes.Where(_ => InvalidResourceScope(_.Name)))
        {
            yield return $"Scope \"{scope.Name}\" of \"{resource.Audience}\" is empty, contains a space or is a protocol scope.";
        }
    }

    static bool InvalidResourceScope(string name) =>
        string.IsNullOrWhiteSpace(name) || name.Contains(' ', StringComparison.Ordinal) || ProtocolScopes.Contains(name);

    static string NormalizeIndicator(string indicator) => ResourceIndicator.Normalize(indicator) ?? indicator;

    static IEnumerable<string> Duplicates(IEnumerable<string> values, string kind) =>
        values.GroupBy(_ => _, StringComparer.Ordinal).Where(_ => _.Count() > 1).Select(_ => $"The {kind} \"{_.Key}\" is configured more than once.");

    static void ValidateAccountLookupKey(string key, List<string> failures)
    {
        if (string.IsNullOrEmpty(key)) return;
        var bytes = new byte[key.Length];
        if (!Convert.TryFromBase64String(key, bytes, out var written) || written < 32)
        {
            failures.Add("Account lookup key must be base64 with at least 256 bits of key material.");
        }
    }

    static bool ValidTenantsCallbackUri(Uri url) =>
        (url.Scheme == "http" || url.Scheme == "https") && string.IsNullOrEmpty(url.UserInfo) && string.IsNullOrEmpty(url.Fragment);

    void ValidateResources(IdentityOptions options, List<string> failures)
    {
        failures.AddRange(Duplicates(options.Resources.Select(_ => _.Audience), "audience"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Indicators.Select(NormalizeIndicator)), "resource indicator"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Scopes.Select(s => s.Name)), "scope"));
        failures.AddRange(Duplicates(options.IntrospectionClients.Select(_ => _.ClientId), "introspection client"));
        failures.AddRange(options.Resources.SelectMany(ValidateResource));
        failures.AddRange(options.Resources.SelectMany(ValidateTenantsCallback));
        failures.AddRange(options.IntrospectionClients
            .Where(_ => string.IsNullOrWhiteSpace(_.ClientId) || string.IsNullOrWhiteSpace(_.ClientSecret))
            .Select(_ => $"Introspection client \"{_.ClientId}\" needs both a client id and a client secret."));
    }

    void ValidateAccounts(AccountsOptions accounts, List<string> failures)
    {
        if (accounts.ReadModelCatchUpTimeout <= TimeSpan.Zero)
        {
            failures.Add("Account read-model catch-up timeout must be positive.");
        }

        if (!environment.IsDevelopment() && string.IsNullOrEmpty(accounts.LookupKey))
        {
            failures.Add("Event-sourced accounts outside Development require a configured account lookup key.");
        }

        if (accounts.LookupKeyRevision < 1 || accounts.LinkHoldMinimum < TimeSpan.Zero)
        {
            failures.Add("Account lookup revision and link hold must be valid.");
        }

        ValidateAccountLookupKey(accounts.LookupKey, failures);
    }

    IEnumerable<string> ValidateTenantFixtures(ResourceServerOptions resource)
    {
        if (!environment.IsDevelopment())
        {
            if (resource.Tenants.Count > 0)
            {
                yield return $"Resource server \"{resource.Audience}\": configured tenants are development fixtures.";
            }

            if (resource.TenantsCallback is null)
            {
                yield return $"Resource server \"{resource.Audience}\" needs a tenants callback outside Development.";
            }
        }
    }

    IEnumerable<string> ValidateTenantsCallback(ResourceServerOptions resource)
    {
        foreach (var failure in ValidateTenantFixtures(resource)) yield return failure;
        if (resource.TenantsCallback is not { } callback)
        {
            yield break;
        }

        if (resource.Tenants.Count > 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has both configured tenants and a tenants callback. Configure one of them.";
        }

        foreach (var failure in ValidateTenantsCallbackUrl(resource.Audience, callback.Url)) yield return failure;

        foreach (var failure in ValidateTenantsCallbackSettings(resource.Audience, callback)) yield return failure;
    }

    IEnumerable<string> ValidateTenantsCallbackSettings(string audience, TenantsCallbackOptions callback)
    {
        if (callback.Timeout <= TimeSpan.Zero || callback.Timeout > TimeSpan.FromSeconds(10))
        {
            yield return $"The tenants callback timeout of \"{audience}\" must be greater than zero and at most ten seconds.";
        }

        if (!environment.IsDevelopment() && (callback.Secret?.Length ?? 0) < 32)
        {
            yield return $"The tenants callback secret of \"{audience}\" must have at least 32 characters outside Development.";
        }
    }

    IEnumerable<string> ValidateTenantsCallbackUrl(string audience, string configured)
    {
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var url) || !ValidTenantsCallbackUri(url))
        {
            yield return $"The tenants callback URL of \"{audience}\" must be an absolute HTTP(S) URI without user info or a fragment.";
        }
        else if (!environment.IsDevelopment() && url.Scheme != "https")
        {
            yield return $"The tenants callback of \"{audience}\" requires HTTPS outside Development.";
        }
    }
}
