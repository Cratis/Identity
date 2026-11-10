// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_certificate_has_no_key : given.a_development_profile
{
    void Establish()
    {
        Apply();
        File.Delete(Directory.EnumerateFiles(_storeDirectory, "signing.*.key").Single());
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_say_the_key_is_missing() => _exception.Message.ShouldContain("no private key");
}
