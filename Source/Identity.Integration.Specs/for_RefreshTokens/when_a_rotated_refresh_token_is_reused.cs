// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_RefreshTokens;

[Requirement(Requirements.RefreshRotation), Requirement(Requirements.RefreshReuseRevokes)]
[Collection(IdentityCollection.Name)]
public class when_a_rotated_refresh_token_is_reused(IdentityHost host) : IAsyncLifetime
{
    string _first;
    HttpStatusCode _rotationStatus;
    string _second;
    HttpStatusCode _reuseStatus;
    JsonElement _reuse;
    HttpStatusCode _successorStatus;
    JsonElement _successorIntrospection;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        _first = tokens.GetProperty("refresh_token").GetString()!;

        (_rotationStatus, var rotated) = await browser.Refresh(_first);
        _second = rotated.GetProperty("refresh_token").GetString()!;
        var secondAccessToken = rotated.GetProperty("access_token").GetString()!;

        (_reuseStatus, _reuse) = await browser.Refresh(_first);
        (_successorStatus, _) = await browser.Refresh(_second);
        (_, _successorIntrospection) = await browser.Introspect(secondAccessToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_rotate_on_refresh() => _rotationStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_a_new_refresh_token() => _second.ShouldNotEqual(_first);
    [Fact] void should_reject_the_reused_token() => _reuseStatus.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_reject_it_as_an_invalid_grant() => _reuse.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_revoke_the_successor_refresh_token() => _successorStatus.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_revoke_the_successor_access_token_for_introspection() => _successorIntrospection.GetProperty("active").GetBoolean().ShouldBeFalse();
}
