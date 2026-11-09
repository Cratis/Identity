// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_confirmation_is_malformed : given.a_validated_access_token
{
    Exception _error;

    void Establish() => _context.GenericTokenPrincipal.SetClaim("cnf", "not-json");
    async Task Because() => _error = await Catch.Exception(Populate);

    [Fact] void should_error_in_openiddict_before_identity_adds_claims() => _error.ShouldNotBeNull();
    [Fact] void should_not_leak_credential_ids() => _context.Claims.ShouldBeEmpty();
}
