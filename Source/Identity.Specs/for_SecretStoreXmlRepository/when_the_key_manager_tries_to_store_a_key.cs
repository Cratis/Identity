// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_SecretStoreXmlRepository;

public class when_the_key_manager_tries_to_store_a_key : given.a_data_protection_ring
{
    void Establish() => Open();

    void Because() => _exception = Catch.Exception(() => _repository.StoreElement(new XElement("key"), "generated"));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<DataProtectionKeysAreProvisioned>();
}
