// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyRingLoader;

public class when_a_data_protection_key_is_malformed : given.a_secret_store
{
    void Establish()
    {
        _files["data-protection.a.xml"] = "<key";
    }

    void Because() => Load();

    [Fact] void should_refuse_the_whole_store() => _exception.ShouldBeOfExactType<InvalidKeyMaterial>();
    [Fact] void should_name_the_file() => Refusal.File.ShouldEqual("data-protection.a.xml");
    [Fact] void should_give_the_reason() => Refusal.Reason.ShouldContain("malformed");
    [Fact] void should_not_leak_key_bytes() => Refusal.Message.ShouldNotContain("PRIVATE KEY");
}
