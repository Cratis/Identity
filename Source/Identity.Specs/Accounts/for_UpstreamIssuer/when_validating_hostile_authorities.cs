// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamIssuer;

public class when_validating_hostile_authorities
{
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(31)]
    [InlineData(127)]
    [InlineData(133)]
    void should_refuse_control_characters_even_in_an_absolute_uri(int character) => Catch.Exception(() => _ = new UpstreamIssuer($"https://issuer.test/path{(char)character}")).ShouldBeOfExactType<InvalidAccountValue>();

    [Fact]
    void should_preserve_a_large_absolute_uri_without_truncating_it()
    {
        var authority = $"https://issuer.test/{new string('a', 65536)}";
        new UpstreamIssuer(authority).Value.ShouldEqual(authority);
    }
}
