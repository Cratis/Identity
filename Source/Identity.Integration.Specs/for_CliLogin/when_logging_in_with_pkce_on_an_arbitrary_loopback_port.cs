// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_CliLogin;

[Collection(IdentityCollection.Name)]
public class when_logging_in_with_pkce_on_an_arbitrary_loopback_port(IdentityHost host) : IAsyncLifetime
{
    string _consentPage;
    Uri _callback;
    HttpStatusCode _tokenStatus;
    JsonElement _tokens;
    TokenValidationResult _accessToken;
    JsonElement _introspection;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();

        using var page = await browser.Authorize(Browser.CliAuthorization(challenge));
        _consentPage = await page.Content.ReadAsStringAsync();

        using var redirect = await browser.Consent(_consentPage);
        _callback = redirect.Headers.Location!;

        var code = QueryHelpers.ParseQuery(_callback.Query)["code"].ToString();
        (_tokenStatus, _tokens) = await browser.RedeemCode(code, verifier);
        _accessToken = await AccessTokens.Validate(browser, _tokens.GetProperty("access_token").GetString()!);
        (_, _introspection) = await browser.Introspect(_tokens.GetProperty("access_token").GetString()!);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_ask_for_consent_naming_the_client() => _consentPage.Contains("Authorize Cratis CLI?", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_ask_for_consent_naming_the_local_redirect() => _consentPage.Contains("a program on this computer (127.0.0.1:53123)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_ask_for_consent_naming_the_resource() => _consentPage.Contains("Cratis Direct", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_redirect_to_the_loopback_port_the_cli_chose() => _callback.GetLeftPart(UriPartial.Path).ShouldEqual(Browser.RedirectUri);
    [Fact] void should_return_the_state() => QueryHelpers.ParseQuery(_callback.Query)["state"].ToString().ShouldEqual("spec-state");
    [Fact] void should_identify_the_issuer_in_the_redirect() => QueryHelpers.ParseQuery(_callback.Query)["iss"].ToString().ShouldEqual(IdentityHost.Issuer);
    [Fact] void should_issue_tokens() => _tokenStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_a_refresh_token() => _tokens.TryGetProperty("refresh_token", out _).ShouldBeTrue();
    [Fact] void should_issue_an_access_token_valid_for_the_audience() => _accessToken.IsValid.ShouldBeTrue();
    [Fact] void should_carry_a_canonical_account_subject() => Guid.TryParse((string)_accessToken.Claims["sub"], out _).ShouldBeTrue();
    [Fact] void should_carry_the_github_id() => _accessToken.Claims["github_id"].ShouldEqual("134365");
    [Fact] void should_carry_the_github_login() => _accessToken.Claims["github_login"].ShouldEqual("einari");
    [Fact] void should_carry_the_selected_tenant() => _accessToken.Claims["tid"].ShouldEqual(IdentityHost.MemberTenant);
    [Fact] void should_carry_the_client_as_authorized_party() => _accessToken.Claims["azp"].ShouldEqual(Browser.ClientId);
    [Fact] void should_carry_the_granted_scope() => ((string)_accessToken.Claims["scope"]).Split(' ').ShouldContain("direct:read");
    [Fact] void should_be_active_when_introspected() => _introspection.GetProperty("active").GetBoolean().ShouldBeTrue();
}
