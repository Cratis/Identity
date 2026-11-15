// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_McpAcceptance.given;

namespace Cratis.Identity.Integration.for_McpAcceptance;

[Collection(DevelopmentProfileCollection.Name)]
public class when_a_token_is_not_active(DevelopmentProfileHost host) : a_signed_in_mcp_client(new LocalMcpAcceptanceHarness(host))
{
    int _callsAfterTwoActiveChecks;
    int _callsAfterWindow;
    int _callsAfterFirstRefusal;
    int _callsAfterSecondRefusal;
    HttpStatusCode _firstRefusal;
    HttpStatusCode _secondRefusal;

    async Task Because()
    {
        await Direct.Call(DirectAccessToken);
        await Direct.Call(DirectAccessToken);
        _callsAfterTwoActiveChecks = DirectIntrospection.Calls;
        Time.Advance(IntrospectingTokenCheck.MaximumPositiveCache + TimeSpan.FromSeconds(1));
        await Direct.Call(DirectAccessToken);
        _callsAfterWindow = DirectIntrospection.Calls;

        await RevokeDirect();
        Time.Advance(IntrospectingTokenCheck.MaximumPositiveCache + TimeSpan.FromSeconds(1));
        _firstRefusal = await Direct.Call(DirectAccessToken);
        _callsAfterFirstRefusal = DirectIntrospection.Calls;
        _secondRefusal = await Direct.Call(DirectAccessToken);
        _callsAfterSecondRefusal = DirectIntrospection.Calls;
    }

    [Fact] void should_introspect_an_active_token_once_inside_the_window() => _callsAfterTwoActiveChecks.ShouldEqual(1);
    [Fact] void should_introspect_again_once_the_window_has_passed() => _callsAfterWindow.ShouldEqual(2);
    [Fact] void should_refuse_the_inactive_token() => _firstRefusal.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_the_inactive_token_again() => _secondRefusal.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_cache_the_refusal() => _callsAfterSecondRefusal.ShouldEqual(_callsAfterFirstRefusal + 1);
}
