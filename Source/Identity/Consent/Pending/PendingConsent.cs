// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.ConsentFlow.Pending;

/// <summary>The package-owned, escaped consent display; contains no credential or continuation bindings.</summary>
/// <param name="Id">The pending request identity.</param>
/// <param name="ClientName">The escaped client identifier, not trusted HTML.</param>
/// <param name="Resource">The escaped resource identifier, never a navigation URI.</param>
/// <param name="Scopes">The exact requested permission display.</param>
/// <param name="Tenants">The eligible tenant display.</param>
/// <param name="ExpiresAt">The request deadline.</param>
/// <param name="Warning">The required interaction warning.</param>
/// <param name="Proof">The session-bound decision proof.</param>
[ReadModel]
internal record PendingConsent(ConsentRequestId Id, string ClientName, string Resource, ConsentScope[] Scopes, ConsentTenant[] Tenants, DateTimeOffset ExpiresAt, string Warning, string Proof)
{
    /// <summary>Gets the newest eligible request for the caller; accepts no target identifiers.</summary>
    /// <param name="interaction">The package-owned interaction boundary.</param>
    /// <returns>The safe display, or null.</returns>
    [Authorize]
    public static Task<PendingConsent?> ForCurrentSession(ConsentInteraction interaction) => interaction.Find(CancellationToken.None);
}
