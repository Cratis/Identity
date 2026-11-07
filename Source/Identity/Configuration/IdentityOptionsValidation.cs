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

        ValidateAccounts(options.Accounts, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
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
        if (!accounts.EventSourced)
        {
            return;
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
