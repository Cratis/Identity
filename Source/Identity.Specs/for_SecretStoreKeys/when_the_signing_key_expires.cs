// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_signing_key_expires : given.a_secret_store_directory
{
    bool _beforeExpiry;
    bool _afterExpiryBeforeReload;
    bool _afterReload;
    string _signerAfterReload;
    bool _afterNewKey;

    void Establish()
    {
        new StoreFiles().Pair("signing", "a", KeyMaterial.Rsa(KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddHours(1))).WriteTo(_directory);
        Open();
    }

    void Because()
    {
        _beforeExpiry = _keys.CanIssue;
        _time.Now = KeyMaterial.Now.AddHours(2);
        _afterExpiryBeforeReload = _keys.CanIssue;
        _keys.Reload();
        _afterReload = _keys.CanIssue;
        _signerAfterReload = _keys.Current.Signer.Name;
        new StoreFiles().Pair("signing", "c", KeyMaterial.Rsa(KeyMaterial.Now.AddHours(-1), KeyMaterial.Now.AddDays(30))).WriteTo(_directory);
        _keys.Reload();
        _afterNewKey = _keys.CanIssue;
    }

    [Fact] void should_issue_while_the_key_is_valid() => _beforeExpiry.ShouldBeTrue();
    [Fact] void should_stop_issuing_the_moment_the_key_expires() => _afterExpiryBeforeReload.ShouldBeFalse();
    [Fact] void should_stay_stopped_after_the_reload_finds_no_usable_key() => _afterReload.ShouldBeFalse();
    [Fact] void should_keep_the_expired_key_only_for_publication() => _signerAfterReload.ShouldEqual("a");
    [Fact] void should_resume_when_a_valid_key_is_provisioned() => _afterNewKey.ShouldBeTrue();
}
