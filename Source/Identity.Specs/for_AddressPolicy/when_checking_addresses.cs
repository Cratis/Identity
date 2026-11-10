// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Clients.MetadataDocuments;

namespace Cratis.Identity.for_AddressPolicy;

public class when_checking_addresses : Specification
{
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("0.0.0.0", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("127.0.0.2", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("192.0.0.8", false)]
    [InlineData("192.0.2.1", false)]
    [InlineData("192.88.99.1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("198.51.100.1", false)]
    [InlineData("203.0.113.1", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("::", false)]
    [InlineData("::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("2001:db8::1", false)]
    [InlineData("2001::1", false)]
    [InlineData("2002:0808:0808::1", false)]
    [InlineData("64:ff9b::808:808", false)]
    [InlineData("64:ff9b:1::808:808", false)]
    [InlineData("::ffff:8.8.8.8", false)]
    [InlineData("::ffff:127.0.0.1", false)]
    [InlineData("::ffff:0:8.8.8.8", false)]
    [InlineData("::8.8.8.8", false)]
    public void should_only_accept_unambiguous_public_addresses(string address, bool accepted) => AddressPolicy.IsPublic(IPAddress.Parse(address)).ShouldEqual(accepted);
}
