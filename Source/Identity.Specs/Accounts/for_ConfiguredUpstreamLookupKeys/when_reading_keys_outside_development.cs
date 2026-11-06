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
        var exception = Catch.Exception(() => Read(material));
        exception.ShouldBeOfExactType<AccountOperationRefused>();
        exception.Message.ShouldEqual("lookup_key_required");
    }

    [Fact]
    void should_make_native_lookup_available_by_default()
    {
        Read(Convert.ToBase64String(new byte[32])).Current.Revision.ShouldEqual(3);
    }

    [Fact]
    void should_use_the_configured_key()
    {
        var bytes = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var expected = new UpstreamLookupKey(3, bytes);
        var configured = Read(Convert.ToBase64String(bytes)).Current;
        configured.Check.ShouldEqual(expected.Check);
        configured.Hash([1, 2, 3]).ShouldEqual(expected.Hash([1, 2, 3]));
    }

    static ConfiguredUpstreamLookupKeys Read(string? material) => new(
        Options.Create(new IdentityOptions { Accounts = new() { LookupKey = material!, LookupKeyRevision = 3 } }),
        new HostingEnvironment { EnvironmentName = Environments.Production });
}
