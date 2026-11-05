// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_TokenStorage;

[Collection(IdentityCollection.Name)]
public class when_a_refresh_token_entry_expired_in_the_store(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _status;
    JsonElement _error;
    JsonElement _accessTokenIntrospection;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;
        await given.ExpiredEntries.Expire(host, "refresh_token");
        (_status, _error) = await browser.Refresh(refreshToken);
        (_, _accessTokenIntrospection) = await browser.Introspect(tokens.GetProperty("access_token").GetString()!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_refuse_the_refresh_token() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_report_invalid_grant() => _error.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_not_revoke_the_grant_because_expiry_is_not_reuse() => _accessTokenIntrospection.GetProperty("active").GetBoolean().ShouldBeTrue();
}
