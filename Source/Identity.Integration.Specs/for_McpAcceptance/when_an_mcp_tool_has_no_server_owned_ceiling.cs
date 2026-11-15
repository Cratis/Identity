// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance;

public class when_an_mcp_tool_has_no_server_owned_ceiling : Specification
{
    public static bool ProductToolHostAvailable => false;

    [Fact(SkipUnless = nameof(ProductToolHostAvailable), Skip = "Pending Direct#2660 and Identity#126: products must refuse tools without a server-owned scope ceiling, independently of token validity. The local resource stand-ins validate and introspect bearer tokens but have no product tool dispatcher or authorization policy.")]
    void should_refuse_the_tool_invocation_even_with_a_valid_product_token() =>
        throw new NotSupportedException("Requires product-owned MCP tool authorization; a stub returning Forbidden would not verify the ceiling.");
}
