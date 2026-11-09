// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Defines the validation of an external issuer's subject token.
/// </summary>
internal interface ISubjectTokenValidator
{
    /// <summary>
    /// Validates a subject token on behalf of a client. Fails closed: anything not positively verified is refused.
    /// </summary>
    /// <param name="rawToken">The compact JWS presented as <c language="text">subject_token</c>.</param>
    /// <param name="client">The authenticated <see cref="TokenExchangeClientOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="SubjectTokenResult"/>.</returns>
    Task<SubjectTokenResult> Validate(string rawToken, TokenExchangeClientOptions client, CancellationToken cancellationToken = default);
}
