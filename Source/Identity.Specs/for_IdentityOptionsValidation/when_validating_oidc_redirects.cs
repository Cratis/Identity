// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_oidc_redirects : given.an_oidc_configuration
{
    [Theory]
    [InlineData("https://rp.example/callback", true)]
    [InlineData("http://127.0.0.1:49152/callback", true)]
    [InlineData("http://[::1]:49152/callback", true)]
    [InlineData("http://localhost/callback", false)]
    [InlineData("http://rp.example/callback", false)]
    [InlineData("http://192.0.2.1/callback", false)]
    [InlineData("/callback", false)]
    [InlineData("file:///callback", false)]
    [InlineData("https://rp.example/callback#fragment", false)]
    [InlineData("https://user@rp.example/callback", false)]
    [InlineData("https://*.example/callback", false)]
    [InlineData("https://rp.example/*", false)]
    [InlineData("", false)]
    void should_apply_the_redirect_policy(string uri, bool valid)
    {
        client.RedirectUris = [uri];
        Validate().Succeeded.ShouldEqual(valid);
    }

    [Theory]
    [InlineData("https://rp.example/logout", true)]
    [InlineData("http://127.0.0.1:49152/logout", true)]
    [InlineData("http://[::1]:49152/logout", true)]
    [InlineData("http://localhost/logout", false)]
    [InlineData("http://rp.example/logout", false)]
    [InlineData("/logout", false)]
    [InlineData("https://rp.example/logout#fragment", false)]
    [InlineData("https://user@rp.example/logout", false)]
    [InlineData("https://rp.example/*", false)]
    void should_apply_the_same_post_logout_policy(string uri, bool valid)
    {
        client.PostLogoutRedirectUris = [uri];
        Validate().Succeeded.ShouldEqual(valid);
    }
}
