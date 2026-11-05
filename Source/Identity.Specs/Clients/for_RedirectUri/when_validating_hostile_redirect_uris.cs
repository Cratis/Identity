// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.for_RedirectUri;

public class when_validating_hostile_redirect_uris
{
    [Theory]
    [InlineData("/callback")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://x/cb")]
    [InlineData("https://user:password@x/cb")]
    [InlineData("https://x/cb?code=redeemable")]
    [InlineData("https://x/cb#frag")]
    void should_refuse_unsafe_redirects(string value) => Catch.Exception(() => RedirectUri.Create(value)).ShouldBeOfExactType<InvalidClientValue>();

    [Fact] void should_refuse_an_overlong_redirect() => Catch.Exception(() => RedirectUri.Create($"https://x/{new string('a', 2048)}")).ShouldBeOfExactType<InvalidClientValue>();

    [Theory]
    [InlineData("http://127.0.0.1/callback")]
    [InlineData("http://[::1]/callback")]
    [InlineData("https://x/cb")]
    void should_accept_safe_redirects(string value) => RedirectUri.Create(value).Value.ShouldEqual(value);
}
