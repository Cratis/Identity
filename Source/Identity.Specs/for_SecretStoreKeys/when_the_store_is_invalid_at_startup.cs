// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_store_is_invalid_at_startup : given.a_secret_store_directory
{
    void Establish() => File.Delete(Path.Combine(_directory, "signing.a.key"));

    void Because() => Open();

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<InvalidKeyMaterial>();
}
