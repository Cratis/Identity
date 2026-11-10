// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_McpAcceptance.given;

namespace Cratis.Identity.Integration.for_McpAcceptance;

[Collection(DevelopmentProfileCollection.Name)]
public class when_product_tokens_cross_resource_boundaries(DevelopmentProfileHost host) : a_signed_in_mcp_client(new LocalMcpAcceptanceHarness(host))
{
    HttpStatusCode _directAtStudio;
    HttpStatusCode _studioAtDirect;
    HttpStatusCode _directAtDirect;
    HttpStatusCode _studioAtStudio;

    async Task Because()
    {
        var directToken = DirectTokens.GetProperty("access_token").GetString();
        var studioToken = StudioTokens.GetProperty("access_token").GetString();
        _directAtDirect = await Direct.Call(directToken);
        _studioAtStudio = await Studio.Call(studioToken);
        _directAtStudio = await Studio.Call(directToken);
        _studioAtDirect = await Direct.Call(studioToken);
    }

    [Fact] void should_accept_the_direct_token_at_its_own_resource() => _directAtDirect.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_the_studio_token_at_its_own_resource() => _studioAtStudio.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_reject_the_direct_token_at_studio() => _directAtStudio.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_reject_the_studio_token_at_direct() => _studioAtDirect.ShouldEqual(HttpStatusCode.Unauthorized);
}
