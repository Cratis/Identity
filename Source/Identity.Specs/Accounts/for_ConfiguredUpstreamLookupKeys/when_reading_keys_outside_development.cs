// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.for_ConfiguredUpstreamLookupKeys;

public class when_reading_keys_outside_development
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    void should_refuse_without_a_configured_key(string? material)
    {
        var exception = Catch.Exception(() => Read(material, eventSourced: true));
        exception.ShouldBeOfExactType<AccountOperationRefused>();
        exception.Message.ShouldEqual("lookup_key_required");
    }

    [Fact]
    void should_refuse_when_event_sourced_accounts_are_not_enabled()
    {
        var exception = Catch.Exception(() => Read(Convert.ToBase64String(new byte[32]), eventSourced: false));
        exception.ShouldBeOfExactType<AccountOperationRefused>();
        exception.Message.ShouldEqual("event_sourced_accounts_not_enabled");
    }

    [Fact]
    void should_use_the_configured_key()
    {
        var bytes = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var expected = new UpstreamLookupKey(3, bytes);
        var configured = Read(Convert.ToBase64String(bytes), eventSourced: true).Current;
        configured.Check.ShouldEqual(expected.Check);
        configured.Hash([1, 2, 3]).ShouldEqual(expected.Hash([1, 2, 3]));
    }

    static ConfiguredUpstreamLookupKeys Read(string? material, bool eventSourced) => new(
        Options.Create(new IdentityOptions { Accounts = new() { EventSourced = eventSourced, LookupKey = material!, LookupKeyRevision = 3 } }),
        new HostingEnvironment { EnvironmentName = Environments.Production });
}
