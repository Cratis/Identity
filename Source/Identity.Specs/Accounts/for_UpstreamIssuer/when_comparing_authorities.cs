// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamIssuer;

public class when_comparing_authorities
{
    [Theory]
    [InlineData("https://example.test/A", "https://example.test/a")]
    [InlineData("https://example.test", "https://example.test/")]
    [InlineData("https://EXAMPLE.test", "https://example.test")]
    void should_preserve_exact_authority(string first, string second) => new UpstreamIssuer(first).ShouldNotEqual(new UpstreamIssuer(second));

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    void should_refuse_relative_authority(string value) => Catch.Exception(() => _ = new UpstreamIssuer(value)).ShouldBeOfExactType<InvalidAccountValue>();
}
