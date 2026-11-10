// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_issuer_is_not_loopback : given.a_development_profile
{
    void Establish() => _settings["Identity:Issuer"] = "https://id.example.com/";

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_create_no_keys() => Directory.EnumerateFileSystemEntries(_root).ShouldBeEmpty();
}
