// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_the_token_is_not_an_access_token : given.a_validated_access_token
{
    void Establish() => _context.GenericTokenPrincipal.SetTokenType(TokenTypeIdentifiers.RefreshToken);
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_not_add_access_token_credential_metadata() => _context.Claims.ShouldBeEmpty();
}
