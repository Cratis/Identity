// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_directory_holds_a_deployed_key_ring : given.a_development_profile
{
    const string DeployedKey = "signing.prod-2026.key";

    void Establish()
    {
        Directory.CreateDirectory(_storeDirectory);
        File.WriteAllText(Path.Combine(_storeDirectory, DeployedKey), "deployed");
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_not_adopt_it() => Directory.EnumerateFiles(_storeDirectory).Select(Path.GetFileName).ShouldContainOnly(DeployedKey);
    [Fact] void should_not_leak_its_content() => _exception.Message.Contains("deployed\"").ShouldBeFalse();
}
