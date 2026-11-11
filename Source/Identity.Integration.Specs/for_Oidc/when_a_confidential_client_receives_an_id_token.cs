// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcOpenIdScope)]
[Collection(OidcCollection.Name)]
public class when_a_confidential_client_receives_an_id_token(OidcIdentityHost host) : an_oidc_login(host)
{
    string? _nonOidcError;
    string? _ceilingError;
    string? _unknownError;
    string? _resourceError;

    async Task Because()
    {
        var (_, challenge) = Browser.Pkce();
        _nonOidcError = await Error(Browser.CliAuthorization(challenge, scope: "openid"));
        _ceilingError = await Error(OidcRequests.Authorization(challenge, SpecClientDescriptors.OidcOpenIdOnly, "openid profile"));
        _unknownError = await Error(OidcRequests.Authorization(challenge, scope: "openid unknown"));
        var resource = OidcRequests.Authorization(challenge);
        resource.Add(new("resource", IdentityHost.DirectMcp));
        _resourceError = await Error(resource);
    }

    async Task<string?> Error(IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        using var response = await _browser.Authorize(parameters);
        return await OidcErrors.From(response);
    }

    [Fact] void should_reject_openid_for_the_cli() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _nonOidcError.ShouldEqual("invalid_scope"));
    [Fact] void should_reject_profile_outside_the_client_ceiling() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _ceilingError.ShouldEqual("invalid_scope"));
    [Fact] void should_reject_unknown_scopes() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _unknownError.ShouldEqual("invalid_scope"));
    [Fact] void should_reject_identity_scopes_with_a_resource() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _resourceError.ShouldEqual("invalid_target"));
    [Fact] void should_issue_an_identity_token_for_openid() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => string.IsNullOrEmpty(_idToken).ShouldBeFalse());
    [Fact] void should_not_treat_it_as_a_resource_grant() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _identity.TryGetProperty("tenant_id", out _).ShouldBeFalse());
    [Fact] void should_publish_bearer_token_type() => Conformance.Conformance.Expect(Requirements.OidcOpenIdScope, () => _tokens.GetProperty("token_type").GetString().ShouldEqual("Bearer"));
}
