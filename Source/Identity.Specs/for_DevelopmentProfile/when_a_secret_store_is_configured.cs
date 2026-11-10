// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_secret_store_is_configured : given.a_development_profile
{
    void Establish() => _settings["Identity:SecretStore:Path"] = "/run/secrets/identity";

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_create_no_keys() => Directory.EnumerateFileSystemEntries(_root).ShouldBeEmpty();
    [Fact] void should_keep_the_configured_store() => _startupOptions.SecretStore.Path.ShouldEqual("/run/secrets/identity");
}
