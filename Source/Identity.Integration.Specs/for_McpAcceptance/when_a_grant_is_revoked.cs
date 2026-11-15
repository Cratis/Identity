// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_McpAcceptance.given;

namespace Cratis.Identity.Integration.for_McpAcceptance;

[Collection(DevelopmentProfileCollection.Name)]
public class when_a_grant_is_revoked(DevelopmentProfileHost host) : a_signed_in_mcp_client(new LocalMcpAcceptanceHarness(host))
{
    static readonly TimeSpan _pastTheCacheWindow = IntrospectingTokenCheck.MaximumPositiveCache + TimeSpan.FromSeconds(1);

    HttpStatusCode _directBefore;
    HttpStatusCode _studioBefore;
    HttpStatusCode _directRevocation;
    HttpStatusCode _directAfterWindow;
    HttpStatusCode _studioWhileOnlyDirectIsRevoked;
    HttpStatusCode _studioRevocation;
    HttpStatusCode _studioAfterWindow;

    async Task Because()
    {
        _directBefore = await Direct.Call(DirectAccessToken);
        _studioBefore = await Studio.Call(StudioAccessToken);
        _directRevocation = await RevokeDirect();

        // Inside the window a cached positive answer may still be served; only the 30 second bound is asserted.
        Time.Advance(_pastTheCacheWindow);
        _directAfterWindow = await Direct.Call(DirectAccessToken);
        _studioWhileOnlyDirectIsRevoked = await Studio.Call(StudioAccessToken);
        _studioRevocation = await RevokeStudio();
        Time.Advance(_pastTheCacheWindow);
        _studioAfterWindow = await Studio.Call(StudioAccessToken);
    }

    [Fact] void should_accept_the_direct_token_before_revocation() => _directBefore.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_the_studio_token_before_revocation() => _studioBefore.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_revoke_the_direct_grant() => _directRevocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_the_revoked_direct_token_once_the_cache_window_has_passed() => _directAfterWindow.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_still_accept_the_studio_token_while_only_direct_is_revoked() => _studioWhileOnlyDirectIsRevoked.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_revoke_the_studio_grant() => _studioRevocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_the_revoked_studio_token_once_the_cache_window_has_passed() => _studioAfterWindow.ShouldEqual(HttpStatusCode.Unauthorized);
}
