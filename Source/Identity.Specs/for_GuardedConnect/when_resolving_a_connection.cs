// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Clients.MetadataDocuments;

namespace Cratis.Identity.for_GuardedConnect;

public class when_resolving_a_connection : Specification
{
    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("::ffff:8.8.8.8")]
    public async Task should_refuse_private_or_translated_answers_without_connecting(string address)
    {
        var resolver = Substitute.For<IHostResolver>();
        var connector = Substitute.For<IAddressConnector>();
        resolver.ResolveAsync("client.example", Arg.Any<CancellationToken>()).Returns([IPAddress.Parse(address)]);
        var connect = new GuardedConnect(resolver, connector);
        await Assert.ThrowsAsync<HttpRequestException>(async () => await connect.ConnectAsync("client.example", 443, CancellationToken.None));
        await connector.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_refuse_mixed_answers_before_any_connection()
    {
        var resolver = Substitute.For<IHostResolver>();
        var connector = Substitute.For<IAddressConnector>();
        resolver.ResolveAsync("client.example", Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("8.8.8.8"), IPAddress.Loopback]);
        await Assert.ThrowsAsync<HttpRequestException>(async () => await new GuardedConnect(resolver, connector).ConnectAsync("client.example", 443, CancellationToken.None));
        await connector.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_connect_only_to_the_checked_answer_and_refuse_a_rebound_answer()
    {
        var resolver = Substitute.For<IHostResolver>();
        var connector = Substitute.For<IAddressConnector>();
        var address = IPAddress.Parse("8.8.8.8");
        resolver.ResolveAsync("client.example", Arg.Any<CancellationToken>()).Returns([address], [IPAddress.Loopback]);
        connector.ConnectAsync(address, 443, Arg.Any<CancellationToken>()).Returns(new MemoryStream());
        var connect = new GuardedConnect(resolver, connector);
        await using var stream = await connect.ConnectAsync("client.example", 443, CancellationToken.None);
        await Assert.ThrowsAsync<HttpRequestException>(async () => await connect.ConnectAsync("client.example", 443, CancellationToken.None));
        await resolver.Received(2).ResolveAsync("client.example", Arg.Any<CancellationToken>());
        await connector.Received(1).ConnectAsync(address, 443, Arg.Any<CancellationToken>());
        connector.ReceivedCalls().Count().ShouldEqual(1);
    }

    [Fact]
    void should_disable_transport_bypasses()
    {
        using var handler = new GuardedConnect(Substitute.For<IHostResolver>(), Substitute.For<IAddressConnector>()).CreateHandler(TimeSpan.FromSeconds(5));
        handler.AllowAutoRedirect.ShouldBeFalse();
        handler.UseProxy.ShouldBeFalse();
        handler.UseCookies.ShouldBeFalse();
        handler.Credentials.ShouldBeNull();
        handler.PreAuthenticate.ShouldBeFalse();
        handler.AutomaticDecompression.ShouldEqual(DecompressionMethods.None);
        handler.SslOptions.EnabledSslProtocols.ShouldEqual(System.Security.Authentication.SslProtocols.None);
        handler.SslOptions.RemoteCertificateValidationCallback.ShouldBeNull();
        handler.ConnectCallback.ShouldNotBeNull();
    }
}
