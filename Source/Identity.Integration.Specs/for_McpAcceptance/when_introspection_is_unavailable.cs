// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_McpAcceptance.given;

namespace Cratis.Identity.Integration.for_McpAcceptance;

[Collection(DevelopmentProfileCollection.Name)]
public class when_introspection_is_unavailable(DevelopmentProfileHost host) : a_signed_in_mcp_client(new LocalMcpAcceptanceHarness(host))
{
    HttpStatusCode _cachedWhileUnavailable;
    Dictionary<IntrospectionBehavior, HttpStatusCode> _expiredCacheOutcomes;

    async Task Because()
    {
        await Direct.Call(DirectAccessToken);
        DirectIntrospection.Behavior = IntrospectionBehavior.Unreachable;
        _cachedWhileUnavailable = await Direct.Call(DirectAccessToken);

        _expiredCacheOutcomes = [];
        foreach (var behavior in new[] { IntrospectionBehavior.Unreachable, IntrospectionBehavior.ServerError, IntrospectionBehavior.Garbage, IntrospectionBehavior.MissingActive })
        {
            Time.Advance(IntrospectingTokenCheck.MaximumPositiveCache + TimeSpan.FromSeconds(1));
            DirectIntrospection.Behavior = behavior;
            _expiredCacheOutcomes[behavior] = await Direct.Call(DirectAccessToken);
        }
    }

    [Fact] void should_only_serve_the_cached_answer_inside_the_window() => _cachedWhileUnavailable.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_when_identity_is_unreachable() => _expiredCacheOutcomes[IntrospectionBehavior.Unreachable].ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_when_identity_answers_with_an_error() => _expiredCacheOutcomes[IntrospectionBehavior.ServerError].ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_when_the_answer_is_not_json() => _expiredCacheOutcomes[IntrospectionBehavior.Garbage].ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_when_the_answer_does_not_say_whether_the_token_is_active() => _expiredCacheOutcomes[IntrospectionBehavior.MissingActive].ShouldEqual(HttpStatusCode.Unauthorized);
}
