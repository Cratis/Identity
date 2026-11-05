// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_TokenStorage;

[Collection(IdentityCollection.Name)]
public class when_an_authorization_code_entry_expired_in_the_store(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _status;
    JsonElement _error;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var code = await browser.LoginForCode(challenge);
        await given.ExpiredEntries.Expire(host, "authorization_code");
        (_status, _error) = await browser.RedeemCode(code, verifier);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_refuse_the_code() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_report_invalid_grant() => _error.GetProperty("error").GetString().ShouldEqual("invalid_grant");
}
