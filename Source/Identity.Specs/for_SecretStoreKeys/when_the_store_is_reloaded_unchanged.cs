// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_store_is_reloaded_unchanged : given.a_secret_store_directory
{
    int _fired;

    void Establish()
    {
        Open();
        _keys.ChangeToken.RegisterChangeCallback(_ => _fired++, null);
    }

    void Because() => _keys.Reload();

    [Fact] void should_not_fire_the_change_token() => _fired.ShouldEqual(0);
}
