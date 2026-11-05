// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;

namespace Cratis.Identity.for_SecretStoreXmlRepository;

public class when_the_ring_is_missing : given.a_data_protection_ring
{
    void Establish() => _files.Remove("data-protection.a.xml");

    void Because() => _exception = Catch.Exception(Open);

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<InvalidKeyMaterial>();
}
