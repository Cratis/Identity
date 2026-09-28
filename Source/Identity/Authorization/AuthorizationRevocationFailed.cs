// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// The exception that is thrown when an authorization could not be revoked after its refresh token was reused.
/// The request fails without issuing tokens.
/// </summary>
/// <param name="authorizationId">The authorization identifier.</param>
public class AuthorizationRevocationFailed(string authorizationId)
    : Exception($"The authorization \"{authorizationId}\" could not be revoked after its refresh token was reused.");
