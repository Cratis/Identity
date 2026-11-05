// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_reading_the_secret_store : given.a_secret_store_directory
{
    void Because() => Open();

    [Fact] void should_start() => _exception.ShouldBeNull();
    [Fact] void should_sign_with_the_key_from_the_directory() => _keys.Current.Signer.Name.ShouldEqual("a");
    [Fact] void should_encrypt_with_the_key_from_the_directory() => _keys.Current.Encrypter.Name.ShouldEqual("a");
    [Fact] void should_hold_the_data_protection_ring_from_the_directory() => _keys.Current.Ring.DataProtection.Count.ShouldEqual(1);
}
