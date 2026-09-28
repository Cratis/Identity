// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Defines the rules that decide which resource server and scopes an authorization request is granted.
/// </summary>
public interface IGrantPolicy
{
    /// <summary>
    /// Evaluates an authorization request.
    /// </summary>
    /// <param name="request">The <see cref="GrantRequest"/>.</param>
    /// <returns>The <see cref="GrantEvaluation"/>.</returns>
    GrantEvaluation Evaluate(GrantRequest request);

    /// <summary>
    /// Evaluates the resource indicators of a token request (code redemption or refresh) against the
    /// audience the grant was bound to at authorization time.
    /// </summary>
    /// <param name="resourceIndicators">The RFC 8707 resource indicators of the token request.</param>
    /// <param name="grantedAudiences">The audiences the grant is bound to.</param>
    /// <returns><see langword="null"/> when the token request is acceptable; otherwise the <see cref="NotGrantable"/> to return.</returns>
    NotGrantable? EvaluateTokenRequest(IEnumerable<string> resourceIndicators, IEnumerable<string> grantedAudiences);
}
