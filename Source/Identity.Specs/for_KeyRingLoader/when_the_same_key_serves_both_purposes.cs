// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyRingLoader;

public class when_the_same_key_serves_both_purposes : given.a_secret_store
{
    void Establish()
    {
        _files.Pair("encryption", "a", new PemPair(_files["signing.a.crt"], _files["signing.a.key"]));
    }

    void Because() => Load();

    [Fact] void should_refuse_the_whole_store() => _exception.ShouldBeOfExactType<InvalidKeyMaterial>();
    [Fact] void should_name_the_file() => Refusal.File.ShouldEqual("signing.a.crt");
    [Fact] void should_give_the_reason() => Refusal.Reason.ShouldContain("serves both");
    [Fact] void should_not_leak_key_bytes() => Refusal.Message.ShouldNotContain("PRIVATE KEY");
}
