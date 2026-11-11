// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

public class an_oidc_login(OidcIdentityHost host) : Specification
{
    protected Browser _browser;
    protected OidcIdentityHost Host => host;
    protected JsonElement _tokens;
    protected JsonElement _identity;
    protected string _idToken;
    protected string _accessToken;
    protected virtual string ClientId => SpecClientDescriptors.OidcWeb;
    protected virtual string Scope => "openid profile";

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        var result = await OidcRequests.Login(_browser, ClientId, Scope);
        if (result.Status != HttpStatusCode.OK) throw new InvalidOperationException("OIDC code redemption did not succeed.");
        _tokens = result.Body;
        _idToken = _tokens.GetProperty("id_token").GetString();
        _accessToken = _tokens.GetProperty("access_token").GetString();
        _identity = OidcTokens.Payload(_idToken);
    }

    void Destroy() => _browser?.Client.Dispose();
}
