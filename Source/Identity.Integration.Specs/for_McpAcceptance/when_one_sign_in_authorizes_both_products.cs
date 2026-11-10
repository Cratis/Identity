// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_McpAcceptance.given;

namespace Cratis.Identity.Integration.for_McpAcceptance;

[Collection(DevelopmentProfileCollection.Name)]
public class when_one_sign_in_authorizes_both_products(DevelopmentProfileHost host) : a_signed_in_mcp_client(new LocalMcpAcceptanceHarness(host))
{
    HttpStatusCode _directStatus;
    HttpStatusCode _studioStatus;

    async Task Because()
    {
        _directStatus = await Direct.Call(DirectTokens.GetProperty("access_token").GetString());
        _studioStatus = await Studio.Call(StudioTokens.GetProperty("access_token").GetString());
    }

    [Fact] void should_authorize_the_direct_resource() => _directStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_authorize_the_studio_resource_without_another_upstream_sign_in() => _studioStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_separate_refresh_grants() => DirectTokens.GetProperty("refresh_token").GetString().ShouldNotEqual(StudioTokens.GetProperty("refresh_token").GetString());
}
