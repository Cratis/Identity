// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_TokenStorage.given;
using Microsoft.IdentityModel.JsonWebTokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Integration.for_Introspection;

[Requirement(Requirements.IntrospectionAuthenticated), Requirement(Requirements.IntrospectionInactiveAfterRevoke)]
[Collection(IdentityCollection.Name)]
public class when_tracking_a_grant_through_introspection(IdentityHost host) : IAsyncLifetime
{
    JsonElement _active;
    JsonElement _refreshed;
    JsonElement _revoked;
    JsonElement _revokedAgain;
    JsonElement _expired;
    JsonWebToken _firstToken;
    JsonWebToken _nextToken;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        _firstToken = new(tokens.GetProperty("access_token").GetString()!);
        (_, _active) = await browser.Introspect(_firstToken.EncodedToken);
        var (refreshStatus, refreshed) = await browser.Refresh(tokens.GetProperty("refresh_token").GetString());
        refreshStatus.ShouldEqual(HttpStatusCode.OK);
        _nextToken = new(refreshed.GetProperty("access_token").GetString()!);
        (_, _refreshed) = await browser.Introspect(_nextToken.EncodedToken);
        var (revocationStatus, _) = await browser.Revoke(refreshed.GetProperty("refresh_token").GetString());
        revocationStatus.ShouldEqual(HttpStatusCode.OK);
        (_, _revoked) = await browser.Introspect(_nextToken.EncodedToken);
        (_, _revokedAgain) = await browser.Introspect(_nextToken.EncodedToken);

        var nextBrowser = host.CreateBrowser();
        var (nextVerifier, nextChallenge) = Browser.Pkce();
        var (_, nextTokens) = await nextBrowser.RedeemCode(await nextBrowser.LoginForCode(nextChallenge), nextVerifier);
        await ExpiredEntries.Expire(host, TokenTypeIdentifiers.AccessToken);
        (_, _expired) = await nextBrowser.Introspect(nextTokens.GetProperty("access_token").GetString());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_be_active_on_code_redemption() => _active.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_a_canonical_grant_uuid() => Guid.TryParseExact(_active.GetProperty("credential_id").GetString(), "D", out _).ShouldBeTrue();
    [Fact] void should_report_the_wire_kind() => _active.GetProperty("credential_kind").GetString().ShouldEqual("oauth-grant");
    [Fact] void should_be_active_after_refresh() => _refreshed.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_grant_uuid_after_refresh() => _refreshed.GetProperty("credential_id").GetString().ShouldEqual(_active.GetProperty("credential_id").GetString());
    [Fact] void should_match_the_first_advisory_jwt_cid() => _firstToken.GetClaim("cid").Value.ShouldEqual(_active.GetProperty("credential_id").GetString());
    [Fact] void should_match_the_refreshed_advisory_jwt_cid() => _nextToken.GetClaim("cid").Value.ShouldEqual(_refreshed.GetProperty("credential_id").GetString());
    [Fact] void should_not_use_jti_as_the_credential_id() => (_active.GetProperty("credential_id").GetString() == _firstToken.Id).ShouldBeFalse();
    [Fact] void should_not_use_client_id_as_the_credential_id() => (_active.GetProperty("credential_id").GetString() == Browser.ClientId).ShouldBeFalse();
    [Fact] void should_return_the_token_tenant() => _active.GetProperty("tid").GetString().ShouldEqual(_firstToken.GetClaim("tid").Value);
    [Fact] void should_return_the_granted_scope() => _active.GetProperty("scope").GetString()!.Split(' ').ShouldContainOnly(_firstToken.GetClaim("scope").Value.Split(' '));
    [Fact] void should_report_a_nonnegative_cache_epoch() => (_active.GetProperty("cache_epoch").GetInt64() >= 0).ShouldBeTrue();
    [Fact] void should_keep_the_epoch_without_a_recovery_boundary() => _refreshed.GetProperty("cache_epoch").GetInt64().ShouldEqual(_active.GetProperty("cache_epoch").GetInt64());
    [Fact] void should_not_fabricate_bearer_confirmation() => _active.TryGetProperty("cnf", out _).ShouldBeFalse();
    [Fact] void should_return_only_inactive_after_revocation() => IsOnlyInactive(_revoked).ShouldBeTrue();
    [Fact] void should_return_only_inactive_on_repeated_revoked_introspection() => IsOnlyInactive(_revokedAgain).ShouldBeTrue();
    [Fact] void should_return_only_inactive_for_an_expired_entry() => IsOnlyInactive(_expired).ShouldBeTrue();

    static bool IsOnlyInactive(JsonElement response) => response.EnumerateObject().Count() == 1 && !response.GetProperty("active").GetBoolean();
}
