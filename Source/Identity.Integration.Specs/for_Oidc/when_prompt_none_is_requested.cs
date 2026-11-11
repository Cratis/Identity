// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Collection(OidcCollection.Name)]
public class when_prompt_none_is_requested(OidcIdentityHost host) : an_oidc_login(host)
{
    string? _signedIn;
    string? _signedOut;
    string? _mixed;

    async Task Because()
    {
        var (_, challenge) = Browser.Pkce();
        var parameters = OidcRequests.Authorization(challenge);
        parameters.Add(new("prompt", "none"));
        using var authenticated = await _browser.Authorize(parameters);
        _signedIn = await OidcErrors.From(authenticated);
        var fresh = Host.CreateBrowser();
        using var client = fresh.Client;
        using var anonymous = await fresh.Authorize(parameters);
        _signedOut = await OidcErrors.From(anonymous);
        var mixed = OidcRequests.Authorization(challenge);
        mixed.Add(new("prompt", "login none"));
        using var invalid = await fresh.Authorize(mixed);
        _mixed = await OidcErrors.From(invalid);

        // OpenIddict refuses an invalid prompt combination before the redirect URI is validated, so the error is local.
        if (_mixed is null && (int)invalid.StatusCode == 400)
        {
            var body = await invalid.Content.ReadAsStringAsync();
            _mixed = body.Contains("invalid_request", StringComparison.Ordinal) ? "invalid_request" : body;
        }
    }

    [Fact] void should_require_consent_without_remembered_approval() => _signedIn.ShouldEqual("consent_required");
    [Fact] void should_require_login_without_a_session() => _signedOut.ShouldEqual("login_required");
    [Fact] void should_reject_none_combined_with_login() => _mixed.ShouldEqual("invalid_request");
}
