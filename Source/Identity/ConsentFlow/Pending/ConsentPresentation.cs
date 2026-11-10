// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.ConsentFlow.Pending;

/// <summary>Renders escaped display text and raw scope identifiers, never HTML, logos or navigation URIs.</summary>
internal static class ConsentPresentation
{
    /// <summary>Builds the safe package-owned display from validated server inputs.</summary>
    /// <param name="request">The operational request.</param>
    /// <param name="eligible">The current catalogue inputs.</param>
    /// <param name="proof">The protected decision proof.</param>
    /// <returns>The escaped display.</returns>
    internal static PendingConsent Create(ConsentRequest request, ConsentEligibility eligible, string proof) => new(
        request.Id,
        WebUtility.HtmlEncode(eligible.ClientName ?? request.Binding.Client),
        WebUtility.HtmlEncode(eligible.Grant.Resource.DisplayName),
        request.Binding.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(scope => Scope(scope, eligible)).ToArray(),
        eligible.Tenants.Select(tenant => new ConsentTenant(tenant.Id, WebUtility.HtmlEncode(tenant.Name))).ToArray(),
        request.Deadline,
        "Only continue if you started this.",
        proof);

    static ConsentScope Scope(string scope, ConsentEligibility eligible) => new(
        scope,
        WebUtility.HtmlEncode(eligible.Grant.Resource.Scopes.FirstOrDefault(_ => _.Name == scope)?.Description ?? scope));
}
