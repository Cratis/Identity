// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.for_ConfiguredUpstreamLookupKeys;

public class when_reading_configured_keys
{
    [Theory]
    [InlineData("not-base64!", 1)]
    [InlineData("AA==", 1)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 1)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", 0)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", -1)]
    void should_refuse_invalid_key_configuration(string material, int revision)
    {
        var exception = Catch.Exception(() => Read(material, revision));
        exception.ShouldBeOfExactType<AccountOperationRefused>();
        exception.Message.ShouldEqual("invalid_lookup_key_configuration");
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    void should_use_the_explicit_material_without_falling_back(int size)
    {
        var bytes = Enumerable.Range(0, size).Select(value => (byte)value).ToArray();
        var expected = new UpstreamLookupKey(7, bytes);
        var configured = Read(Convert.ToBase64String(bytes), 7).Current;
        configured.Check.ShouldEqual(expected.Check);
        configured.Hash([1, 2, 3]).ShouldEqual(expected.Hash([1, 2, 3]));
    }

    static ConfiguredUpstreamLookupKeys Read(string material, int revision) => new(
        Options.Create(new IdentityOptions { Accounts = new() { LookupKey = material, LookupKeyRevision = revision } }),
        new HostingEnvironment { EnvironmentName = Environments.Development });
}
