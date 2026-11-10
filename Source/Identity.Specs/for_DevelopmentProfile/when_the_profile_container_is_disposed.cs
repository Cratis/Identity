// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_profile_container_is_disposed : given.a_development_profile
{
    void Because()
    {
        Apply();
        _exception.ShouldBeNull();
        using (var provider = _services.BuildServiceProvider())
        {
            provider.GetRequiredService<DevelopmentKeyStoreRoot>().ShouldEqual(_custody);
        }

        _exception = Catch.Exception(() => _custody.ReadKeyFiles());
    }

    [Fact] void should_release_the_process_lifetime_custody() => _exception.ShouldBeOfExactType<ObjectDisposedException>();
}
