// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_content_root_is_the_key_directory : given.a_development_profile
{
    void Because() => _exception = Catch.Exception(() => new DevelopmentKeyStore(_root, _storeDirectory, TimeProvider.System).Provision(StoreName));

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_create_nothing() => Directory.Exists(_storeDirectory).ShouldBeFalse();
}
