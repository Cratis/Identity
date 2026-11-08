// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Hosting;

/// <summary>
/// Adds value-free startup diagnostics for the opt-in hosted-shared deployment preset.
/// </summary>
/// <param name="configuration">The host configuration.</param>
/// <param name="environment">The host environment.</param>
internal sealed class HostedSharedConfiguration(IConfiguration configuration, IHostEnvironment environment) : IValidateOptions<IdentityOptions>
{
    static readonly string[] _requiredSettings =
    [
        "Identity:GitHub:ClientId", "Identity:GitHub:ClientSecret",
        "Identity:MongoDB:ConnectionString", "Identity:MongoDB:Database",
        "Identity:FenceLog:ConnectionString", "Identity:FenceLog:Database", "Identity:FenceLog:InstanceId",
        "Cratis:Chronicle:ConnectionString"
    ];

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, IdentityOptions options)
    {
        if (environment.IsDevelopment() || configuration["Identity:Hosting:Preset"] != "hosted-shared")
        {
            return ValidateOptionsResult.Skip;
        }

        var failures = ValidateIssuer(options).Concat(ValidateRequiredSettings()).Concat(ValidateCustody())
            .Concat(ValidateHostOptions(options)).Concat(ValidateResources(options)).ToList();
        var accountValidation = new IdentityOptionsValidation(environment).Validate(name, new IdentityOptions { Accounts = options.Accounts });
        if (accountValidation.Failed)
        {
            failures.AddRange(accountValidation.Failures);
        }

        // The general validator can quote identifiers or URLs. Do not propagate those values into startup diagnostics.
        if (new IdentityOptionsValidation(environment).Validate(name, options).Failed && failures.Count == 0)
        {
            failures.Add("Identity configuration must satisfy IdentityOptionsValidation (resources, callbacks, accounts and token settings).");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    static IEnumerable<string> ValidateIssuer(IdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            yield return new IssuerNotConfigured().Message;
        }
        else if (!IsHttpsOrigin(options.Issuer))
        {
            yield return "Identity:Issuer must be the HTTPS public origin, ending with a slash.";
        }
    }

    static IEnumerable<string> ValidateHostOptions(IdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretStore.Path))
        {
            yield return new SecretStoreNotConfigured().Message;
        }

        if (options.SecurityOwners.All(string.IsNullOrWhiteSpace))
        {
            yield return "Identity:SecurityOwners must contain at least one explicitly provisioned subject.";
        }

        if (options.Tokens.AccessTokenLifetime <= TimeSpan.Zero || options.Tokens.AccessTokenLifetime > TimeSpan.FromMinutes(5))
        {
            yield return "Identity:Tokens:AccessTokenLifetime must be positive and no greater than 00:05:00 for hosted-shared.";
        }
    }

    static IEnumerable<string> ValidateResources(IdentityOptions options)
    {
        if (options.Resources.Count == 0)
        {
            yield return "Identity:Resources must contain the product resource entries.";
        }

        for (var index = 0; index < options.Resources.Count; index++)
        {
            var resource = options.Resources[index];
            var prefix = $"Identity:Resources:{index}";
            if (resource.Tenants.Count > 0)
            {
                yield return $"{prefix}:Tenants are Development fixtures and must not be configured for hosted-shared.";
            }

            if (resource.Indicators.All(string.IsNullOrWhiteSpace))
            {
                yield return $"{prefix}:Indicators must contain a resource indicator for resource server \"{resource.Audience}\" ({prefix}:Indicators:0).";
            }

            foreach (var failure in ValidateCallback(resource.TenantsCallback, prefix))
            {
                yield return failure;
            }
        }
    }

    static IEnumerable<string> ValidateCallback(TenantsCallbackOptions? callback, string prefix)
    {
        if (callback is null)
        {
            yield return $"{prefix}:TenantsCallback must be configured for hosted-shared.";
            yield break;
        }

        if (!IsHttpsCallback(callback.Url))
        {
            yield return $"{prefix}:TenantsCallback:Url must be HTTPS without user info or a fragment.";
        }

        if ((callback.Secret?.Length ?? 0) < 32)
        {
            yield return $"{prefix}:TenantsCallback:Secret must have at least 32 characters.";
        }
    }

    static bool IsHttpsOrigin(string value) => Uri.TryCreate(value, UriKind.Absolute, out var issuer) &&
        IsHttpsWithoutUserInfo(issuer) && value == $"{issuer.GetLeftPart(UriPartial.Authority)}/";

    static bool IsHttpsCallback(string value) => Uri.TryCreate(value, UriKind.Absolute, out var url) &&
        IsHttpsWithoutUserInfo(url) && string.IsNullOrEmpty(url.Fragment);

    static bool IsHttpsWithoutUserInfo(Uri value) => value.Scheme == "https" && string.IsNullOrEmpty(value.UserInfo);

    IEnumerable<string> ValidateRequiredSettings() => _requiredSettings.Where(key => string.IsNullOrWhiteSpace(configuration[key]))
        .Select(key => $"{key} must be configured for hosted-shared.");

    IEnumerable<string> ValidateCustody()
    {
        var connection = configuration["Identity:FenceLog:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connection) && connection == configuration["Identity:MongoDB:ConnectionString"])
        {
            yield return "Identity:FenceLog:ConnectionString must differ from Identity:MongoDB:ConnectionString and use independent custody.";
        }

        if (!long.TryParse(configuration["Identity:FenceLog:MinimumSequence"], out var minimum) || minimum < 0)
        {
            yield return "Identity:FenceLog:MinimumSequence must be an explicitly supplied nonnegative independent allocation bound.";
        }
    }
}
