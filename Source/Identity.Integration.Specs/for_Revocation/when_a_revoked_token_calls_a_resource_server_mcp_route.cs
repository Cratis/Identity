// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Revocation;

public class when_a_revoked_token_calls_a_resource_server_mcp_route
{
    /// <summary>Gets a value indicating whether the edge introspection behavior is available; false until Cratis/AuthProxy#255 ships.</summary>
    public static bool EdgeIntrospectionAvailable => false;

    [Fact(SkipUnless = nameof(EdgeIntrospectionAvailable), Skip = "Blocked on Cratis/AuthProxy#255: the edge must introspect with a positive cache of at most 30 seconds on /mcp. See Documentation/security/resource-token-validation.md.")]
    public void should_refuse_the_request_at_the_edge() => throw new NotImplementedException();
}
