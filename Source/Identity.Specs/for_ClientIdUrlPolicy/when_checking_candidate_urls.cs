// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.MetadataDocuments;

namespace Cratis.Identity.for_ClientIdUrlPolicy;

public class when_checking_candidate_urls : Specification
{
    [Theory]
    [InlineData("https://client.example/cimd.json", true)]
    [InlineData("https://client.example:443/cimd.json", false)]
    [InlineData("https://CLIENT.example/cimd.json", false)]
    [InlineData("https://client.example/a/../cimd.json", false)]
    [InlineData("https://client.example/%2e%2e/cimd.json", false)]
    [InlineData("https://client/cimd.json", false)]
    [InlineData("https://client.internal/cimd.json", false)]
    [InlineData("https://client.123/cimd.json", false)]
    [InlineData("https://client.0x7f/cimd.json", false)]
    [InlineData("https://café.example/cimd.json", false)]
    [InlineData("https://xn--caf-dma.example/cimd.json", true)]
    [InlineData("http://client.example/cimd.json", false)]
    [InlineData("https://127.0.0.1/cimd.json", false)]
    [InlineData("https://[::1]/cimd.json", false)]
    [InlineData("https://2130706433/cimd.json", false)]
    [InlineData("https://0x7f000001/cimd.json", false)]
    [InlineData("https://0177.0.0.1/cimd.json", false)]
    [InlineData("https://localhost/cimd.json", false)]
    [InlineData("https://a.local/cimd.json", false)]
    [InlineData("https://client.example./cimd.json", false)]
    [InlineData("https://client.example:8443/cimd.json", false)]
    [InlineData("https://client.example/", false)]
    [InlineData("https://user@client.example/cimd.json", false)]
    [InlineData("https://client.example/cimd.json?token=synthetic", false)]
    [InlineData("https://client.example/cimd.json#part", false)]
    [InlineData(" https://client.example/cimd.json", false)]
    [InlineData("https://client.example/ci\nmd.json", false)]
    [InlineData("https://client.example\\cimd.json", false)]
    public void should_apply_the_url_boundary(string candidate, bool accepted) => ClientIdUrlPolicy.IsValid(candidate).ShouldEqual(accepted);

    [Fact] void should_refuse_an_overlong_url() => ClientIdUrlPolicy.IsValid("https://client.example/" + new string('a', 2048)).ShouldBeFalse();
}
