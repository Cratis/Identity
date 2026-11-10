// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_data_root_is_inside_the_content_root : given.a_development_profile
{
    DevelopmentKeyStore _store;

    void Establish() => _store = new DevelopmentKeyStore(Path.Combine(_contentRoot, "data"), _contentRoot, TimeProvider.System);

    void Because() => _exception = Catch.Exception(() => _store.Provision(StoreName));

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_create_nothing() => Directory.Exists(Path.Combine(_contentRoot, "data")).ShouldBeFalse();
}
