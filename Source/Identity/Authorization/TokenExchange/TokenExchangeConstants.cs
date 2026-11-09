// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Holds the RFC 8693 identifiers the token-exchange grant uses.
/// </summary>
internal static class TokenExchangeConstants
{
    /// <summary>
    /// The grant type of a token-exchange request.
    /// </summary>
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    /// <summary>
    /// The token type of an access token, accepted as subject token type and requested token type, and returned as the issued token type.
    /// </summary>
    public const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";

    /// <summary>
    /// The token type of a JWT, accepted as subject token type.
    /// </summary>
    public const string JwtTokenType = "urn:ietf:params:oauth:token-type:jwt";

    /// <summary>
    /// The <c language="text">subject_token</c> request parameter.
    /// </summary>
    public const string SubjectToken = "subject_token";

    /// <summary>
    /// The <c language="text">subject_token_type</c> request parameter.
    /// </summary>
    public const string SubjectTokenType = "subject_token_type";

    /// <summary>
    /// The <c language="text">actor_token</c> request parameter, which is refused: no delegation is offered.
    /// </summary>
    public const string ActorToken = "actor_token";

    /// <summary>
    /// The <c language="text">requested_token_type</c> request parameter.
    /// </summary>
    public const string RequestedTokenType = "requested_token_type";

    /// <summary>
    /// The <c language="text">audience</c> request parameter, which is refused: the target is named by <c language="text">resource</c>.
    /// </summary>
    public const string Audience = "audience";

    /// <summary>
    /// The <c language="text">tenant</c> request parameter.
    /// </summary>
    public const string Tenant = "tenant";

    /// <summary>
    /// The <c language="text">issued_token_type</c> response parameter.
    /// </summary>
    public const string IssuedTokenType = "issued_token_type";
}
