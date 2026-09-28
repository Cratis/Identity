// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Configuration;

/// <summary>
/// Validates <see cref="IdentityOptions"/> at startup, so a misconfigured resource, scope or tenant stops the
/// service instead of silently narrowing or widening what tokens can carry.
/// </summary>
public class IdentityOptionsValidation : IValidateOptions<IdentityOptions>
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

        failures.AddRange(Duplicates(options.Resources.Select(_ => _.Audience), "audience"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Indicators.Select(i => ResourceIndicator.Normalize(i) ?? i)), "resource indicator"));
        failures.AddRange(Duplicates(options.Resources.SelectMany(_ => _.Scopes.Select(s => s.Name)), "scope"));
        failures.AddRange(Duplicates(options.IntrospectionClients.Select(_ => _.ClientId), "introspection client"));
        failures.AddRange(options.Resources.SelectMany(ValidateResource));
        failures.AddRange(options.IntrospectionClients
            .Where(_ => string.IsNullOrWhiteSpace(_.ClientId) || string.IsNullOrWhiteSpace(_.ClientSecret))
            .Select(_ => $"Introspection client \"{_.ClientId}\" needs both a client id and a client secret."));

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
}
