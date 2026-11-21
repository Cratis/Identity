// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Hosting;
using Cratis.Monads;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Resources.Registering;

/// <summary>Registers one resource identity with permanently reserved catalogue values.</summary>
/// <param name="ResourceIndicator">The single HTTPS indicator.</param>
/// <param name="Audience">The unique audience.</param>
/// <param name="ScopePrefix">The resource's scope namespace.</param>
/// <param name="DisplayName">The Internal business label.</param>
[Command, ExcludeFromDiscovery]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RegisterResource(string ResourceIndicator, string Audience, string ScopePrefix, ResourceDisplayName DisplayName)
{
    /// <summary>Returns an Internal source fact, never the Public publication contract.</summary>
    /// <param name="catalog">The trusted configured catalogue.</param>
    /// <returns>The source fact on a new opaque resource stream, or a closed refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(IResourceCatalog catalog)
    {
        var indicator = ResourceRegistrationValidation.NormalizeIndicator(ResourceIndicator);
        if (!ResourceRegistrationValidation.IsRegistration(indicator, Audience, ScopePrefix, DisplayName?.Value))
        {
            return ValidationResult.Error("invalid_resource_registration");
        }

        if (Conflicts(catalog.All, indicator!, Audience, ScopePrefix))
        {
            return ValidationResult.Error("resource_already_reserved");
        }

        return new EventForEventSourceId[]
        {
            new(ResourceId.New().Value, new ResourceRegistrationRecorded(indicator!, Audience, ScopePrefix, DisplayName!, FactId.New()))
        };
    }

    /// <summary>Refuses configured indicators, audiences and scope namespaces, including normalized aliases.</summary>
    /// <param name="configured">The configured resources.</param>
    /// <param name="indicator">The normalized indicator.</param>
    /// <param name="audience">The audience.</param>
    /// <param name="prefix">The scope prefix.</param>
    /// <returns>Whether configuration already owns any part of the registration.</returns>
    internal static bool Conflicts(IEnumerable<ResourceServer> configured, string indicator, string audience, string prefix) => configured.Any(resource =>
        resource.Audience == audience ||
        resource.Indicators.Any(existing => ResourceRegistrationValidation.NormalizeIndicator(existing) == indicator) ||
        resource.Scopes.Any(scope => scope.Name.StartsWith($"{prefix}:", StringComparison.Ordinal)));
}
