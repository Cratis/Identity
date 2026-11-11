// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcNonce)]
[Collection(OidcCollection.Name)]
public class when_nonce_is_required_and_echoed(OidcIdentityHost host) : an_oidc_login(host)
{
    string _missingNonceError;

    async Task Because()
    {
        var fresh = host.CreateBrowser();
        using var client = fresh.Client;
        var (_, challenge) = Browser.Pkce();
        using var response = await fresh.Authorize(OidcRequests.Authorization(challenge, nonce: null));
        _missingNonceError = await OidcErrors.From(response);
    }

    [Fact] void should_echo_the_original_nonce() => Conformance.Conformance.Expect(Requirements.OidcNonce, () => _identity.GetProperty("nonce").GetString().ShouldEqual(OidcRequests.Nonce));
    [Fact] void should_refuse_a_missing_nonce_before_login() => Conformance.Conformance.Expect(Requirements.OidcNonce, () => _missingNonceError.ShouldEqual("invalid_request"));
}
