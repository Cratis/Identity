// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources;

/// <summary>Trusted configured reservations, never accepted from a command caller.</summary>
/// <param name="Resources">The configured catalogue snapshot.</param>
internal sealed record ResourceRegistrationDecision(IReadOnlyList<ResourceServer> Resources)
{
    /// <summary>Refuses configured indicators, audiences and scope namespaces, including normalized aliases.</summary>
    /// <param name="indicator">The normalized indicator.</param>
    /// <param name="audience">The audience.</param>
    /// <param name="prefix">The scope prefix.</param>
    /// <returns>Whether configuration already owns any part of the registration.</returns>
    public bool Conflicts(string indicator, string audience, string prefix) => Resources.Any(resource =>
        resource.Audience == audience ||
        resource.Indicators.Any(configured => ResourceRegistrationValidation.NormalizeIndicator(configured) == indicator) ||
        resource.Scopes.Any(scope => scope.Name.StartsWith($"{prefix}:", StringComparison.Ordinal)));
}
