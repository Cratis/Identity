// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_store_has_no_active_data_protection_key : given.a_secret_store_directory
{
    void Establish() => File.WriteAllText(
        Path.Combine(_directory, "data-protection.a.xml"),
        KeyMaterial.DataProtectionKey(KeyMaterial.Now.AddDays(1), KeyMaterial.Now.AddDays(90)));

    void Because() => Open();

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<InvalidKeyMaterial>();
    [Fact] void should_say_which_ring_is_not_active() => ((InvalidKeyMaterial)_exception).Reason.ShouldContain("Data Protection");
}
