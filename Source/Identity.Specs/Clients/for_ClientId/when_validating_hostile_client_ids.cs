// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.for_ClientId;

public class when_validating_hostile_client_ids
{
    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("control\u0001")]
    [InlineData("non-ascii-æ")]
    void should_refuse_unsafe_identifiers(string value) => Catch.Exception(() => ClientId.Create(value)).ShouldBeOfExactType<InvalidClientValue>();

    [Fact] void should_refuse_an_overlong_identifier() => Catch.Exception(() => ClientId.Create(new string('a', 256))).ShouldBeOfExactType<InvalidClientValue>();
    [Fact] void should_accept_the_printable_ascii_boundaries() => ClientId.Create("!~").Value.ShouldEqual("!~");
    [Fact] void should_accept_the_maximum_length() => ClientId.Create(new string('a', 255)).Value.Length.ShouldEqual(255);
}
