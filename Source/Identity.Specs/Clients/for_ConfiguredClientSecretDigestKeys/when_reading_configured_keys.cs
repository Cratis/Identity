// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.for_ConfiguredClientSecretDigestKeys;

public class when_reading_configured_keys
{
    [Fact]
    void should_use_the_configured_key()
    {
        var bytes = Enumerable.Repeat((byte)23, 32).ToArray();
        var keys = Create(Environments.Production, Convert.ToBase64String(bytes));
        keys.Current.Hash([1, 2, 3]).ShouldEqual(new ClientSecretDigestKey(7, bytes).Hash([1, 2, 3]));
        keys.Current.Revision.ShouldEqual(7);
    }

    [Fact]
    void should_keep_a_development_key_within_the_process()
    {
        var keys = Create(Environments.Development);
        keys.Current.Hash([1, 2, 3]).ShouldEqual(keys.Current.Hash([1, 2, 3]));
        keys.Current.Hash([1, 2, 3]).ShouldEqual(Create(Environments.Development).Current.Hash([1, 2, 3]));
        keys.Current.Hash([1, 2, 3]).ShouldNotEqual(new ClientSecretDigestKey(7, new byte[32]).Hash([1, 2, 3]));
    }

    [Fact] void should_fail_closed_outside_development() => Catch.Exception(() => Create(Environments.Production)).ShouldBeOfExactType<InvalidOperationException>();

    static ConfiguredClientSecretDigestKeys Create(string environment, string? key = null) => new(
        Options.Create(new IdentityOptions { Clients = new() { SecretDigestKey = key, SecretDigestKeyRevision = 7 } }),
        new HostingEnvironment { EnvironmentName = environment });
}
