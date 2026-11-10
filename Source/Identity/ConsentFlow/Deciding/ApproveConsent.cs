// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Approves only an existing, session-bound package request; never activates or issues anything.</summary>
/// <param name="ConsentRequestId">The request identity.</param>
/// <param name="SelectedTenantId">The selected eligible tenant.</param>
/// <param name="SelectedScopes">Exactly the requested scopes; narrowing is not supported in v1.</param>
/// <param name="Proof">The protected package-issued decision proof.</param>
[Command, Authorize]
internal record ApproveConsent(ConsentRequestId ConsentRequestId, string SelectedTenantId, string[]? SelectedScopes, string? Proof)
{
    /// <summary>Consumes the pending decision once, without evidence or activation side effects.</summary>
    /// <param name="interaction">The package-owned decision boundary.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="ConsentDecisionRefused">The request cannot be approved.</exception>
    public Task Handle(ConsentInteraction interaction) => interaction.Decide(ConsentRequestId, SelectedTenantId, SelectedScopes, Proof, true, CancellationToken.None);
}
