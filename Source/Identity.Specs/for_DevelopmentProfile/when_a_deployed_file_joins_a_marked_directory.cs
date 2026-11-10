// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_deployed_file_joins_a_marked_directory : given.a_development_profile
{
    void Establish()
    {
        Apply();
        File.WriteAllText(Path.Combine(_storeDirectory, "signing.prod-2026.crt"), "deployed");
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}
