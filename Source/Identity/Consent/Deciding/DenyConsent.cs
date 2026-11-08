// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Denies only an existing, session-bound package request.</summary>
/// <param name="ConsentRequestId">The request identity.</param>
/// <param name="Proof">The protected package-issued decision proof.</param>
[Command, Authorize]
internal record DenyConsent(ConsentRequestId ConsentRequestId, string? Proof)
{
    /// <summary>Consumes a pending request into terminal denial without evidence or grants.</summary>
    /// <param name="interaction">The package-owned decision boundary.</param>
    /// <returns>The completion.</returns>
    /// <exception cref="ConsentDecisionRefused">The request cannot be denied.</exception>
    public Task Handle(ConsentInteraction interaction) => interaction.Decide(ConsentRequestId, null, null, Proof, false, CancellationToken.None);
}
