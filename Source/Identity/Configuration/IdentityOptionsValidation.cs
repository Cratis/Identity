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

        if (!string.IsNullOrWhiteSpace(options.Issuer))
        {
            if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) || issuer.Scheme is not ("http" or "https"))
            {
                failures.Add($"Issuer \"{options.Issuer}\" is not an absolute HTTP URI.");
            }
            else if (!string.Equals(options.Issuer, $"{issuer.GetLeftPart(UriPartial.Authority)}/", StringComparison.Ordinal))
            {
                // Discovery is served at the root of the public origin, not under an issuer path prefix.
                failures.Add($"Issuer \"{options.Issuer}\" must be the root of its origin and end with a slash.");
            }
        }

        if (options.SecretStore.ReloadInterval < TimeSpan.FromSeconds(1) || options.SecretStore.ReloadInterval > TimeSpan.FromMinutes(5))
        {
            failures.Add("The secret store reload interval must be between one second and five minutes.");
        }

        failures.AddRange(Duplicates(options.Resources.Select(_ => _.Audience), "audience"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Indicators.Select(i => ResourceIndicator.Normalize(i) ?? i)), "resource indicator"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Scopes.Select(s => s.Name)), "scope"));
        failures.AddRange(Duplicates(options.IntrospectionClients.Select(_ => _.ClientId), "introspection client"));
        failures.AddRange(options.Resources.SelectMany(ValidateResource));
        failures.AddRange(options.Resources.SelectMany(ValidateTenantsCallback));
        failures.AddRange(options.IntrospectionClients
            .Where(_ => string.IsNullOrWhiteSpace(_.ClientId) || string.IsNullOrWhiteSpace(_.ClientSecret))
            .Select(_ => $"Introspection client \"{_.ClientId}\" needs both a client id and a client secret."));

        ValidateDPoP(options.DPoP, environment.IsDevelopment(), failures);
        ValidateDeviceGrant(options.DeviceGrant, failures);
        ValidateAccounts(options.Accounts, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
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
        if (device.UserCodeLength is < 8 or > 32 || string.IsNullOrEmpty(device.UserCodeCharset) ||
            device.UserCodeCharset.Distinct().Count() != device.UserCodeCharset.Length || device.UserCodeCharset.Length < 20 ||
            device.UserCodeCharset.Any(_ => !char.IsAsciiLetterUpper(_) && !char.IsAsciiDigit(_)))
        {
            failures.Add("Identity:Device user codes require 8–32 characters from at least twenty distinct uppercase ASCII letters or digits.");
        }

        if (device.CodeLifetime < TimeSpan.FromMinutes(1) || device.CodeLifetime > TimeSpan.FromMinutes(15))
        {
            failures.Add("The device code lifetime must be between one and fifteen minutes.");
        }

        if (device.PollingInterval < TimeSpan.FromSeconds(5) || device.PollingInterval > TimeSpan.FromSeconds(60) || device.PollingInterval.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            failures.Add("The device polling interval must be a whole number of seconds between five and sixty.");
        }

        if (device.VerificationWindow < TimeSpan.FromMinutes(1) || device.VerificationWindow > TimeSpan.FromHours(1) ||
            device.AttemptsPerCode is < 1 or > 10 || device.AttemptsPerAccount is < 1 or > 100 || device.AttemptsPerIp is < 1 or > 300 ||
            device.AttemptsGlobally is < 1 or > 1000 || device.IssuancesPerIp is < 1 or > 100)
        {
            failures.Add("Device verification needs a one-to-sixty-minute window and bounded positive code, account, IP, global and issuance limits.");
        }
    }

    static IEnumerable<string> ValidateResource(ResourceServerOptions resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Audience))
        {
            yield return "A resource server has no audience.";
        }

        if (resource.Indicators.Count == 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has no resource indicators.";
        }

        foreach (var indicator in resource.Indicators.Where(_ => ResourceIndicator.Normalize(_) is null))
        {
            yield return $"Resource indicator \"{indicator}\" of \"{resource.Audience}\" is not an absolute URI without a fragment.";
        }

        if (resource.Scopes.Count == 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has no scopes.";
        }

        foreach (var scope in resource.Scopes.Where(_ => string.IsNullOrWhiteSpace(_.Name) || _.Name.Contains(' ', StringComparison.Ordinal) || ProtocolScopes.Contains(_.Name)))
        {
            yield return $"Scope \"{scope.Name}\" of \"{resource.Audience}\" is empty, contains a space or is a protocol scope.";
        }

        foreach (var tenant in resource.Tenants.Where(_ => string.IsNullOrWhiteSpace(_.Id)))
        {
            yield return $"A tenant of \"{resource.Audience}\" named \"{tenant.Name}\" has no id.";
        }
    }

    static IEnumerable<string> Duplicates(IEnumerable<string> values, string kind) =>
        values.GroupBy(_ => _, StringComparer.Ordinal).Where(_ => _.Count() > 1).Select(_ => $"The {kind} \"{_.Key}\" is configured more than once.");

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

        if (!string.IsNullOrEmpty(accounts.LookupKey))
        {
            var bytes = new byte[accounts.LookupKey.Length];
            if (!Convert.TryFromBase64String(accounts.LookupKey, bytes, out var written) || written < 32)
            {
                failures.Add("Account lookup key must be base64 with at least 256 bits of key material.");
            }
        }
    }

    IEnumerable<string> ValidateTenantsCallback(ResourceServerOptions resource)
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

        if (resource.TenantsCallback is not { } callback)
        {
            yield break;
        }

        if (resource.Tenants.Count > 0)
        {
            yield return $"Resource server \"{resource.Audience}\" has both configured tenants and a tenants callback. Configure one of them.";
        }

        if (!Uri.TryCreate(callback.Url, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Fragment))
        {
            yield return $"The tenants callback URL of \"{resource.Audience}\" must be an absolute HTTP(S) URI without user info or a fragment.";
        }
        else if (!environment.IsDevelopment() && url.Scheme != "https")
        {
            yield return $"The tenants callback of \"{resource.Audience}\" requires HTTPS outside Development.";
        }

        if (callback.Timeout <= TimeSpan.Zero || callback.Timeout > TimeSpan.FromSeconds(10))
        {
            yield return $"The tenants callback timeout of \"{resource.Audience}\" must be greater than zero and at most ten seconds.";
        }

        if (!environment.IsDevelopment() && (callback.Secret?.Length ?? 0) < 32)
        {
            yield return $"The tenants callback secret of \"{resource.Audience}\" must have at least 32 characters outside Development.";
        }
    }
}
