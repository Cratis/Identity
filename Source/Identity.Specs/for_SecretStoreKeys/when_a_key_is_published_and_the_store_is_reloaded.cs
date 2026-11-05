// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Microsoft.Extensions.Primitives;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_a_key_is_published_and_the_store_is_reloaded : given.a_secret_store_directory
{
    IChangeToken _token;
    int _fired;
    string _signerBefore;

    void Establish()
    {
        Open();
        _token = _keys.ChangeToken;
        _token.RegisterChangeCallback(_ => _fired++, null);
        _signerBefore = _keys.Current.Signer.Kid;
        new StoreFiles().Pair("signing", "b", KeyMaterial.Rsa(KeyMaterial.Now.AddMinutes(-2), KeyMaterial.Now.AddDays(100))).WriteTo(_directory);
    }

    void Because() => _keys.Reload();

    [Fact] void should_fire_the_change_token() => _fired.ShouldEqual(1);
    [Fact] void should_hand_out_a_new_token() => _keys.ChangeToken.HasChanged.ShouldBeFalse();
    [Fact] void should_publish_both_keys() => _keys.Current.Ring.Signing.Count.ShouldEqual(2);
    [Fact] void should_not_sign_with_the_pre_published_key() => _keys.Current.Signer.Kid.ShouldEqual(_signerBefore);
}
