// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Confirms live account/session/grant fence state; absence, errors and uncertainty must deny.</summary>
public interface IRevocationFence
{
    /// <summary>Confirms this exact binding is not fenced and its session remains live.</summary>
    /// <param name="request">The operational request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True only for a confirmed live, unfenced binding.</returns>
    Task<bool> IsClear(ConsentRequest request, CancellationToken cancellationToken);
}
